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
        private record JobOperationItem(ResizePictureItems Item, JobOperation JobOperation);

        public const string QueueName = "picture-resize-queue";

        public async Task Consume(ConsumeContext<ResizePictureEvent> consumeContext)
        {
            try
            {
                var message = consumeContext.Message;
                var cancellationToken = consumeContext.CancellationToken;

                var job = await GetOrCreateJobAsync(message, cancellationToken);
                var jobOperations = await CreateJobOperationAsync(message.Items, job.Id, cancellationToken);

                await database.SaveChangesAsync(cancellationToken);

                var sourceImageResult = await GetSourceImageAsync(message.BucketName, message.KeyName, cancellationToken);
                if (!sourceImageResult.IsSuccess)
                {
                    MarkAllAsFailed(jobOperations, $"Failed to retrieve source image from storage, ResultState: {sourceImageResult.Status}");
                    await database.SaveChangesAsync(cancellationToken);
                    return;
                }

                using var sourceImage = sourceImageResult.Value;

                foreach (var operationItem in jobOperations)
                {
                    await ProcessOperationAsync(
                        operationItem,
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

        private async Task<List<JobOperationItem>> CreateJobOperationAsync(
            IReadOnlyCollection<ResizePictureItems> resizeItems,
            Guid jobId,
            CancellationToken cancellationToken)
        {
            var jobOperations = new List<JobOperationItem>(resizeItems.Count);

            foreach (var resizeItem in resizeItems)
            {
                var jobOperation = new JobOperation
                {
                    JobId = jobId
                };

                await database.JobOperations.AddAsync(jobOperation, cancellationToken);
                jobOperations.Add(new JobOperationItem(resizeItem, jobOperation));
            }

            return jobOperations;
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

        private async Task ProcessOperationAsync(
            JobOperationItem operationItem,
            string bucketName,
            MemoryStream sourceImage,
            CancellationToken cancellationToken)
        {
            var jobOperation = operationItem.JobOperation;

            try
            {
                jobOperation.JobState = JobState.InProgress;
                sourceImage.Position = 0;

                var resizedPictureResult = await pictureHandler.ResizePictureAsync(
                    sourceImage,
                    operationItem.Item.Width,
                    operationItem.Item.Height,
                    cancellationToken);

                if (!resizedPictureResult.IsSuccess)
                {
                    var errorMessage = string.Join("; ", resizedPictureResult.Errors);
                    logger.LogError("Failed to resize picture for JobOperationId={JobOperationId}. Error: {ErrorMessage}",
                        jobOperation.Id,
                        errorMessage);

                    MarkAsFailed(jobOperation, errorMessage);
                    return;
                }

                var saveResult = await storageHandler.UploadFileAsync(
                    resizedPictureResult.Value,
                    "image/webp",
                    bucketName,
                    $"{operationItem.Item.SaveOnRoute}/{Guid.NewGuid()}.webp",
                    cancellationToken);

                if (!saveResult.IsSuccess)
                {
                    var errorMessage = string.Join("; ", saveResult.Errors);
                    logger.LogError("Failed to upload resized picture for JobOperationId={JobOperationId}. Error: {ErrorMessage}",
                        jobOperation.Id,
                        errorMessage);

                    MarkAsFailed(jobOperation, errorMessage);
                    return;
                }

                jobOperation.JobState = JobState.Completed;
                jobOperation.FinishedAt = DateTime.UtcNow;
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "An error occurred while processing resize for JobOperationId={JobOperationId}, Width={Width}, Height={Height}, SaveOnRoute={SaveOnRoute}",
                    jobOperation.Id,
                    operationItem.Item.Width,
                    operationItem.Item.Height,
                    operationItem.Item.SaveOnRoute);

                MarkAsFailed(jobOperation, exception.Message);
            }
        }

        private static void MarkAllAsFailed(IEnumerable<JobOperationItem> jobOperations, string errorMessage)
        {
            foreach (var operationItem in jobOperations)
            {
                MarkAsFailed(operationItem.JobOperation, errorMessage);
            }
        }

        private static void MarkAsFailed(JobOperation jobOperation, string errorMessage)
        {
            jobOperation.JobState = JobState.Failed;
            jobOperation.FinishedAt = DateTime.UtcNow;
            jobOperation.ErrorMessage = errorMessage;
        }
    }
}