using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Domain.Entities;
using Musify.Infrastructure.MassTransit.Activities.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities
{
    internal class GenerateAudioWorkflowPathsActivity(
        IDatabase database,
        ILogger<GenerateAudioWorkflowPathsActivity> logger)
        : IExecuteActivity<GenerateAudioWorkflowPathsArguments>
    {
        public const string ExecuteEndpointName = "generate-audio-workflow-paths";

        public async Task<ExecutionResult> Execute(ExecuteContext<GenerateAudioWorkflowPathsArguments> executeContext)
        {
            Track track;
            try
            {
                var getTrack = await database.Tracks.SingleOrDefaultAsync(p => p.Id == executeContext.Arguments.TrackId, executeContext.CancellationToken);
                track = getTrack ?? throw new InvalidOperationException($"Track with id {executeContext.Arguments.TrackId} not found.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to retrieve track for TrackId {TrackId}",
                    executeContext.Arguments.TrackId);
                throw;
            }

            try
            {
                track.AudioTranscodeProcessingStatus = ProcessingStatus.Processing;
                var folderName = Guid.NewGuid().ToString();
                var sourceFileName = Path.GetFileName(executeContext.Arguments.SourceKey);
                var workingDirectory = Path.Combine(executeContext.Arguments.TemporaryRootDirectory, folderName);
                var sourceFilePath = Path.Combine(workingDirectory, sourceFileName);

                Directory.CreateDirectory(workingDirectory);
                await database.SaveChangesAsync(executeContext.CancellationToken);

                logger.LogDebug("Generated audio workflow paths in {WorkingDirectory}", workingDirectory);

                return executeContext.CompletedWithVariables(new Dictionary<string, object>
                {
                    [RoutingSlipVariableNames.Workflow.TemporalDirectory] = workingDirectory,
                    [RoutingSlipVariableNames.Audio.WorkingDirectory] = workingDirectory,
                    [RoutingSlipVariableNames.Audio.SourceFilePath] = sourceFilePath,
                    [RoutingSlipVariableNames.Audio.TranscodedDirectory] = workingDirectory
                } );
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to generate audio workflow paths for {SourceKey}",
                    executeContext.Arguments.SourceKey);

                try
                {
                    track.AudioTranscodeProcessingStatus = ProcessingStatus.Failed;
                    await database.SaveChangesAsync(executeContext.CancellationToken);
                }
                catch (Exception dbException)
                {
                    logger.LogError(dbException, "Failed to update ProcessingStatus to Failed for track {TrackId}",
                        executeContext.Arguments.TrackId);
                }

                throw;
            }
        }
    }
}