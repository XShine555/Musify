using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Events;
using Musify.Domain.Entities;
using System.Text.Json;

namespace Musify.Infrastructure.Messaging.Consumers
{
    public class RemoveFileConsumer(IDatabase database, IStorageHandler storageHandler, ILogger<RemoveFileConsumer> logger)
        : IConsumer<RemoveFileEvent>
    {
        public const string QueueName = "file-remove-queue";

        public async Task Consume(ConsumeContext<RemoveFileEvent> consumeContext)
        {
            var payload = JsonSerializer.Serialize(consumeContext.Message);

            var job = await database.Jobs
                .SingleOrDefaultAsync(j => j.JobState == JobState.Failed && j.Payload == payload);

            if (job is null) 
            { 
                job = new Job
                {
                    JobType = JobType.RemoveFile,
                    Payload = payload
                };
                await database.Jobs.AddAsync(job, consumeContext.CancellationToken);
                logger.LogInformation("Created new job with id {JobId} for removing file {BucketName}/{KeyName}", job.Id, consumeContext.Message.BucketName, consumeContext.Message.KeyName);
            }
            else
            {
                job.RetryCount++;
                job.JobState = JobState.Pending;
                database.Jobs.Update(job);
                logger.LogInformation("Retrying job with id {JobId} for removing file {BucketName}/{KeyName}. Retry count: {RetryCount}", job.Id, consumeContext.Message.BucketName, consumeContext.Message.KeyName, job.RetryCount);
            }

            var removeFile = await storageHandler.RemoveFileAsync(
                consumeContext.Message.BucketName,
                consumeContext.Message.KeyName,
                consumeContext.CancellationToken);

            if (!removeFile.IsSuccess)
            {
                job.JobState = JobState.Failed;
                logger.LogInformation("Failed to remove file {BucketName}/{KeyName} for job with id {JobId}. Error: {ErrorMessage}", consumeContext.Message.BucketName, consumeContext.Message.KeyName, job.Id, string.Join("; ", removeFile.Errors));
            }
            else
            {
                job.CompletedAt = DateTime.UtcNow;
                logger.LogInformation("Successfully removed file {BucketName}/{KeyName} for job with id {JobId}.", consumeContext.Message.BucketName, consumeContext.Message.KeyName, job.Id);
            }

            await database.SaveChangesAsync(consumeContext.CancellationToken);
        }
    }
}