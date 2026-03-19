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
            }
            else
            {
                job.RetryCount++;
                job.JobState = JobState.Pending;
                database.Jobs.Update(job);
            }

            var removeFile = await storageHandler.RemoveFileAsync(
                consumeContext.Message.BucketName,
                consumeContext.Message.KeyName,
                consumeContext.CancellationToken);

            if (!removeFile.IsSuccess)
                job.JobState = JobState.Failed;

            await database.SaveChangesAsync(consumeContext.CancellationToken);
        }
    }
}