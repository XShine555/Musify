using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Domain.ValueObjects;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.RoutingSlip;

namespace Musify.Infrastructure.MassTransit.Activities.Audio;

internal class GenerateAudioWorkflowPathsActivity(
    IDatabase database,
    ILogger<GenerateAudioWorkflowPathsActivity> logger)
    : IExecuteActivity<GenerateAudioWorkflowPathsArguments>
{
    public const string ExecuteEndpointName = "generate-audio-workflow-paths";

    public async Task<ExecutionResult> Execute(ExecuteContext<GenerateAudioWorkflowPathsArguments> executeContext)
    {
        var arguments = executeContext.Arguments;
        var track = await TrackAudioStatus.LoadAsync(database, arguments.TrackId, executeContext.CancellationToken);

        try
        {
            track.Audio.TranscodeStatus = ProcessingStatus.Processing;

            var workingDirectory = Path.Combine(arguments.TemporaryRootDirectory, Guid.NewGuid().ToString());
            var sourceFilePath = Path.Combine(workingDirectory, Path.GetFileName(arguments.SourceKey));

            Directory.CreateDirectory(workingDirectory);
            await database.SaveChangesAsync(executeContext.CancellationToken);

            logger.LogDebug("Generated audio workflow paths in {WorkingDirectory}", workingDirectory);

            return executeContext.CompletedWithVariables(new Dictionary<string, object>
            {
                [RoutingSlipVariableNames.Workflow.TemporalDirectory] = workingDirectory,
                [RoutingSlipVariableNames.Audio.SourceFilePath] = sourceFilePath,
                [RoutingSlipVariableNames.Audio.TranscodedDirectory] = workingDirectory
            });
        }
        catch
        {
            await TrackAudioStatus.MarkFailedAsync(database, arguments.TrackId, executeContext.CancellationToken);
            throw;
        }
    }
}
