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
        private record JobExecutionItem(ResizePictureItems Item, JobExecution JobExecution);

        public const string QueueName = "picture-resize-queue";

        public async Task Consume(ConsumeContext<ResizePictureEvent> consumeContext)
        {
            try
            {
                var message = consumeContext.Message;
                var cancellationToken = consumeContext.CancellationToken;

                var job = await GetOrCreateJobAsync(message, cancellationToken);
                var jobExecutions = await CreateJobExecutionsAsync(message.Items, job.Id, cancellationToken);

                await database.SaveChangesAsync(cancellationToken);

                var sourceImageResult = await GetSourceImageAsync(message.BucketName, message.KeyName, cancellationToken);
                if (!sourceImageResult.IsSuccess)
                {
                    MarkAllAsFailed(jobExecutions);
                    await database.SaveChangesAsync(cancellationToken);
                    return;
                }

                using var sourceImage = sourceImageResult.Value;

                foreach (var executionItem in jobExecutions)
                {
                    await ProcessExecutionAsync(
                        executionItem,
                        message.BucketName,
                        sourceImage,
                        cancellationToken);

                    await database.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occurred while consuming the ResizePictureConsumer message.");
                throw;
            }
        }

        private async Task<Job> GetOrCreateJobAsync(ResizePictureEvent message, CancellationToken cancellationToken)
        {
            var job = await database.Jobs
                .SingleOrDefaultAsync(j => j.Id == message.JobId, cancellationToken);

            if (job is not null)
            {
                return job;
            }

            var payloadJson = JsonSerializer.Serialize(message.Items);
            job = new Job
            {
                Id = message.JobId,
                JobType = JobType.ResizePicture,
                Payload = payloadJson
            };

            await database.Jobs.AddAsync(job, cancellationToken);
            return job;
        }

        private async Task<List<JobExecutionItem>> CreateJobExecutionsAsync(
            IReadOnlyCollection<ResizePictureItems> items,
            Guid jobId,
            CancellationToken cancellationToken)
        {
            var jobExecutions = new List<JobExecutionItem>(items.Count);

            foreach (var item in items)
            {
                var jobExecution = new JobExecution
                {
                    JobId = jobId
                };

                await database.JobExecutions.AddAsync(jobExecution, cancellationToken);
                jobExecutions.Add(new JobExecutionItem(item, jobExecution));
            }

            return jobExecutions;
        }

        private async Task<Result<MemoryStream>> GetSourceImageAsync(string bucketName, string keyName, CancellationToken cancellationToken)
        {
            var fileResult = await storageHandler.GetFileAsync(bucketName, keyName, cancellationToken);

            if (fileResult.IsNotFound())
            {
                logger.LogWarning("File not found in storage: BucketName={BucketName}, KeyName={KeyName}", bucketName, keyName);
                return Result.NotFound();
            }

            if (!fileResult.IsSuccess)
            {
                logger.LogError("Failed to read source image from storage: BucketName={BucketName}, KeyName={KeyName}. Errors: {Errors}",
                    bucketName,
                    keyName,
                    string.Join("; ", fileResult.Errors));
                return Result.Error(string.Join("; ", fileResult.Errors));
            }

            using var fileStream = fileResult.Value;
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            return Result.Success(memoryStream);
        }

        private async Task ProcessExecutionAsync(
            JobExecutionItem executionItem,
            string bucketName,
            MemoryStream sourceImage,
            CancellationToken cancellationToken)
        {
            var jobExecution = executionItem.JobExecution;

            try
            {
                jobExecution.JobState = JobState.InProgress;
                sourceImage.Position = 0;

                var resizedPictureResult = await pictureHandler.ResizePictureAsync(
                    sourceImage,
                    executionItem.Item.Width,
                    executionItem.Item.Height,
                    cancellationToken);

                if (!resizedPictureResult.IsSuccess)
                {
                    logger.LogError("Failed to resize picture for JobExecutionId={JobExecutionId}. Error: {ErrorMessage}",
                        jobExecution.Id,
                        string.Join("; ", resizedPictureResult.Errors));

                    MarkAsFailed(jobExecution);
                    return;
                }

                var saveResult = await storageHandler.UploadFileAsync(
                    resizedPictureResult.Value,
                    "image/webp",
                    bucketName,
                    $"{executionItem.Item.SaveOnRoute}/{Guid.NewGuid()}.webp",
                    cancellationToken);

                if (!saveResult.IsSuccess)
                {
                    logger.LogError("Failed to upload resized picture for JobExecutionId={JobExecutionId}. Error: {ErrorMessage}",
                        jobExecution.Id,
                        string.Join("; ", saveResult.Errors));

                    MarkAsFailed(jobExecution);
                    return;
                }

                jobExecution.JobState = JobState.Completed;
                jobExecution.FinishedAt = DateTime.UtcNow;
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "An error occurred while processing resize for JobExecutionId={JobExecutionId}, Width={Width}, Height={Height}, SaveOnRoute={SaveOnRoute}",
                    jobExecution.Id,
                    executionItem.Item.Width,
                    executionItem.Item.Height,
                    executionItem.Item.SaveOnRoute);

                MarkAsFailed(jobExecution);
            }
        }

        private static void MarkAllAsFailed(IEnumerable<JobExecutionItem> jobExecutions)
        {
            foreach (var executionItem in jobExecutions)
            {
                MarkAsFailed(executionItem.JobExecution);
            }
        }

        private static void MarkAsFailed(JobExecution jobExecution)
        {
            jobExecution.JobState = JobState.Failed;
            jobExecution.FinishedAt = DateTime.UtcNow;
        }
    }
}