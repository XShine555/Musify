using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Infrastructure.MassTransit.Arguments;
using Musify.Infrastructure.MassTransit.RoutingSlip;

namespace Musify.Infrastructure.MassTransit.Activities.Pictures;

internal class GeneratePictureWorkflowPathsActivity(
    ILogger<GeneratePictureWorkflowPathsActivity> logger)
    : IExecuteActivity<GeneratePictureWorkflowPathsArguments>
{
    public const string ExecuteEndpointName = "generate-picture-workflow-paths";

    internal const string ResizedPictureFileExtension = ".webp";

    public Task<ExecutionResult> Execute(ExecuteContext<GeneratePictureWorkflowPathsArguments> executeContext)
    {
        var workingDirectory = Path.Combine(executeContext.Arguments.TemporaryRootDirectory, Guid.NewGuid().ToString());

        var sourceFilePath = Path.Combine(
            workingDirectory, Guid.NewGuid().ToString() + Path.GetExtension(executeContext.Arguments.SourceKey));
        var smallPictureFilePath = Path.Combine(workingDirectory, Guid.NewGuid().ToString() + ResizedPictureFileExtension);
        var mediumPictureFilePath = Path.Combine(workingDirectory, Guid.NewGuid().ToString() + ResizedPictureFileExtension);
        var largePictureFilePath = Path.Combine(workingDirectory, Guid.NewGuid().ToString() + ResizedPictureFileExtension);

        Directory.CreateDirectory(workingDirectory);

        logger.LogDebug("Generated picture workflow paths in {WorkingDirectory}", workingDirectory);

        return Task.FromResult(executeContext.CompletedWithVariables(new Dictionary<string, object>
        {
            [RoutingSlipVariableNames.Workflow.TemporalDirectory] = workingDirectory,
            [RoutingSlipVariableNames.Picture.OriginalFilePath] = sourceFilePath,
            [RoutingSlipVariableNames.Picture.SmallResizedFilePath] = smallPictureFilePath,
            [RoutingSlipVariableNames.Picture.MediumResizedFilePath] = mediumPictureFilePath,
            [RoutingSlipVariableNames.Picture.LargeResizedFilePath] = largePictureFilePath
        }));
    }
}
