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
                .SingleOrDefaultAsync(j => j.Id == consumeContext.Message.JobId);

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

            var jobExecution = new JobOperation
            {
                JobId = job.Id
            };
            await database.JobOperations.AddAsync(jobExecution, consumeContext.CancellationToken);

            var removeFile = await storageHandler.RemoveFileAsync(
                consumeContext.Message.BucketName,
                consumeContext.Message.KeyName,
                consumeContext.CancellationToken);

            if (!removeFile.IsSuccess)
            {
                jobExecution.JobState = JobState.Failed;
                logger.LogInformation("Failed to remove file {BucketName}/{KeyName} for jobId={JobId}, JobExecutionId={jobExecutionId}. Error: {ErrorMessage}",
                    consumeContext.Message.BucketName, consumeContext.Message.KeyName, job.Id, jobExecution.Id, string.Join("; ", removeFile.Errors));
            }
            else
            {
                logger.LogInformation("Successfully removed file {BucketName}/{KeyName} for jobId={JobId}, JobExecutionId={jobExecutionId}.",
                   consumeContext.Message.BucketName, consumeContext.Message.KeyName, job.Id, jobExecution.Id);
            }

            jobExecution.FinishedAt = DateTime.UtcNow;
            await database.SaveChangesAsync(consumeContext.CancellationToken);
        }
    }
}