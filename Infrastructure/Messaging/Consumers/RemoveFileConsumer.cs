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
            var message = consumeContext.Message;
            var cancellationToken = consumeContext.CancellationToken;

            var job = await GetOrCreateJobAsync(message, cancellationToken);
            var jobOperation = await CreateJobOperationAsync(job.Id, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);

            try
            {
                jobOperation.JobState = JobState.InProgress;
                await database.SaveChangesAsync(cancellationToken);

                var removeResult = await storageHandler.RemoveFileAsync(
                    message.BucketName,
                    message.KeyName,
                    cancellationToken);

                if (!removeResult.IsSuccess)
                {
                    MarkAsFailed(jobOperation, string.Join("; ", removeResult.Errors));

                    logger.LogError(
                        "Failed to remove file {BucketName}/{KeyName} for JobId={JobId}, JobOperationId={JobOperationId}. Error: {ErrorMessage}",
                        message.BucketName,
                        message.KeyName,
                        job.Id,
                        jobOperation.Id,
                        jobOperation.ErrorMessage);

                    await database.SaveChangesAsync(cancellationToken);
                    return;
                }

                jobOperation.JobState = JobState.Completed;
                jobOperation.FinishedAt = DateTime.UtcNow;

                logger.LogInformation(
                    "Successfully removed file {BucketName}/{KeyName} for JobId={JobId}, JobOperationId={JobOperationId}.",
                    message.BucketName,
                    message.KeyName,
                    job.Id,
                    jobOperation.Id);

                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "An error occurred while consuming RemoveFileConsumer message for JobId={JobId}, BucketName={BucketName}, KeyName={KeyName}",
                    message.JobId,
                    message.BucketName,
                    message.KeyName);
                MarkAsFailed(jobOperation, exception.Message);
                await database.SaveChangesAsync(cancellationToken);

                throw;
            }
        }

        async Task<Job> GetOrCreateJobAsync(RemoveFileEvent message, CancellationToken cancellationToken)
        {
            var job = await database.Jobs
                .SingleOrDefaultAsync(j => j.Id == message.JobId, cancellationToken);

            if (job is not null)
            {
                return job;
            }

            job = new Job
            {
                Id = message.JobId,
                JobType = JobType.RemoveFile,
                Payload = JsonSerializer.Serialize(message)
            };

            await database.Jobs.AddAsync(job, cancellationToken);

            logger.LogInformation(
                "Created new job with id {JobId} for removing file {BucketName}/{KeyName}",
                job.Id,
                message.BucketName,
                message.KeyName);

            return job;
        }

        async Task<JobOperation> CreateJobOperationAsync(Guid jobId, CancellationToken cancellationToken)
        {
            var jobOperation = new JobOperation
            {
                JobId = jobId
            };

            await database.JobOperations.AddAsync(jobOperation, cancellationToken);
            return jobOperation;
        }

        void MarkAsFailed(JobOperation jobOperation, string errorMessage)
        {
            jobOperation.JobState = JobState.Failed;
            jobOperation.ErrorMessage = errorMessage;
            jobOperation.FinishedAt = DateTime.UtcNow;
        }
    }
}