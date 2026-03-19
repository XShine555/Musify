using Ardalis.Result;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Domain.Entities;
using System.Text.Json;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class ResizePictureConsumer(IStorageHandler storageHandler, IPictureHandler pictureHandler, IDatabase database, ILogger<ResizePictureConsumer> logger)
        : IConsumer<ResizePictureEvent>
    {
        public const string QueueName = "picture-resize-queue";

        public async Task Consume(ConsumeContext<ResizePictureEvent> consumeContext)
        {
            try
            {
                Dictionary<ResizePictureItems, Job> jobs = new Dictionary<ResizePictureItems, Job>();

                foreach (var item in consumeContext.Message.Items)
                {
                    var payloadJson = JsonSerializer.Serialize(item);

                    var job = await database.Jobs
                        .SingleOrDefaultAsync(job => job.JobType == JobType.ResizePicture && job.Payload == payloadJson, consumeContext.CancellationToken);

                    if (job is null)
                    {
                        job = new Job
                        {
                            JobType = JobType.ResizePicture,
                            Payload = payloadJson,
                        };
                        await database.Jobs.AddAsync(job, consumeContext.CancellationToken);
                    }
                    else
                    {
                        job.RetryCount++;
                        job.JobState = JobState.InProgress;
                        database.Jobs.Update(job);
                    }

                    jobs.Add(item, job);
                }

                var fileResult = await storageHandler.GetFileAsync(
                    consumeContext.Message.BucketName,
                    consumeContext.Message.KeyName,
                    consumeContext.CancellationToken);

                if (fileResult.IsNotFound())
                {
                    logger.LogWarning("File not found in storage: BucketName={BucketName}, KeyName={KeyName}",
                        consumeContext.Message.BucketName, consumeContext.Message.KeyName);

                    foreach (var job in jobs)
                    {
                        job.Value.JobState = JobState.Failed;
                    }
                    await database.SaveChangesAsync(consumeContext.CancellationToken);
                    return;
                }

                using var fileStream = fileResult.Value;
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, consumeContext.CancellationToken);

                foreach (var item in consumeContext.Message.Items)
                {
                    var job = jobs[item];

                    try
                    {
                        await ResizePicture(
                            job,
                            consumeContext.Message.BucketName,
                            memoryStream,
                            item,
                            consumeContext.CancellationToken);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "An error occurred while resizing picture for UploadId={UploadId} and Resize={Resize}", job.Id, item);
                        continue;
                    }
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occurred while consuming the ImageResizedConsumer message.");
                throw;
            }
            finally 
            {
                logger.LogInformation("Saving changes to database after processing ImageResizedConsumer message.");
                await database.SaveChangesAsync(consumeContext.CancellationToken);
            }
        }

        async Task ResizePicture(Job job, string bucketName, MemoryStream memoryStream, ResizePictureItems items, CancellationToken cancellationToken)
        {
            memoryStream.Position = 0;
            job.JobState = JobState.InProgress;

            var pictureResult = await pictureHandler.ResizePictureAsync(
                memoryStream,
                items.Width,
                items.Height,
                cancellationToken);

            if (!pictureResult.IsSuccess)
            {
                logger.LogError("Failed to resize picture: {ErrorMessage}", string.Join("; ", pictureResult.Errors));
                job.JobState = JobState.Failed;
                return;
            }

            var saveResult = await storageHandler.UploadFileAsync(
                pictureResult.Value,
                "image/webp",
                bucketName,
                $"{items.SaveOnRoute}/{Guid.NewGuid() }.webp",
                cancellationToken);

            if (!saveResult.IsSuccess)
            {
                logger.LogError("Failed to upload resized picture: {ErrorMessage}", string.Join("; ", saveResult.Errors));
                job.JobState = JobState.Failed;
                return;
            }

            job.JobState = JobState.Completed;
        }
    }
}