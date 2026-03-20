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
                Dictionary<ResizePictureItems, JobExecution> jobsExecutions = new Dictionary<ResizePictureItems, JobExecution>();
                var job = await database.Jobs
                    .SingleOrDefaultAsync(job => job.Id == consumeContext.Message.JobId);

                if (job is null)
                {
                    var payloadJson = JsonSerializer.Serialize(consumeContext.Message.Items);
                    job = new Job
                    {
                        Id = consumeContext.Message.JobId,
                        JobType = JobType.ResizePicture,
                        Payload = payloadJson,
                    };
                    await database.Jobs.AddAsync(job, consumeContext.CancellationToken);
                    await database.SaveChangesAsync(consumeContext.CancellationToken);
                }

                foreach (var item in consumeContext.Message.Items)
                {
                    var jobExecution = new JobExecution
                    {
                        JobId = consumeContext.Message.JobId
                    };
                    await database.JobExecutions.AddAsync(jobExecution, consumeContext.CancellationToken);
                    jobsExecutions.Add(item, jobExecution);
                }

                var fileResult = await storageHandler.GetFileAsync(
                    consumeContext.Message.BucketName,
                    consumeContext.Message.KeyName,
                    consumeContext.CancellationToken);

                if (fileResult.IsNotFound())
                {
                    logger.LogWarning("File not found in storage: BucketName={BucketName}, KeyName={KeyName}",
                        consumeContext.Message.BucketName, consumeContext.Message.KeyName);

                    foreach (var jobExecution in jobsExecutions.Values)
                    {
                        jobExecution.JobState = JobState.Failed;
                    }
                    await database.SaveChangesAsync(consumeContext.CancellationToken);
                    return;
                }

                using var fileStream = fileResult.Value;
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, consumeContext.CancellationToken);

                foreach (var item in consumeContext.Message.Items)
                {
                    var jobExecution = jobsExecutions[item];

                    try
                    {
                        await ResizePicture(
                            jobExecution,
                            consumeContext.Message.BucketName,
                            memoryStream,
                            item,
                            consumeContext.CancellationToken);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "An error occurred while resizing picture for UploadId={UploadId} and Resize={Resize}", jobExecution.Id, item);
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

        async Task ResizePicture(JobExecution jobExecution, string bucketName, MemoryStream memoryStream, ResizePictureItems items, CancellationToken cancellationToken)
        {
            memoryStream.Position = 0;
            jobExecution.JobState = JobState.InProgress;

            var pictureResult = await pictureHandler.ResizePictureAsync(
                memoryStream,
                items.Width,
                items.Height,
                cancellationToken);

            if (!pictureResult.IsSuccess)
            {
                logger.LogError("Failed to resize picture: {ErrorMessage}", string.Join("; ", pictureResult.Errors));
                jobExecution.JobState = JobState.Failed;
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
                jobExecution.JobState = JobState.Failed;
                return;
            }

            jobExecution.JobState = JobState.Completed;
            jobExecution.FinishedAt = DateTime.UtcNow;
        }
    }
}