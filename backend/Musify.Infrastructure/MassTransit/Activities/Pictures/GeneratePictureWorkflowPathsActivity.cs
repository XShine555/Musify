using MassTransit;
using Microsoft.Extensions.Logging;
using Musify.Infrastructure.MassTransit.Arguments;

namespace Musify.Infrastructure.MassTransit.Activities
{
    public class GeneratePictureWorkflowPathsActivity(
        ILogger<GeneratePictureWorkflowPathsActivity> logger)
        : IExecuteActivity<GeneratePictureWorkflowPathsArguments>
    {
        public const string ExecuteEndpointName = "generate-picture-workflow-paths";

        internal const string ResizedPictureFileExtension = ".webp";

        public async Task<ExecutionResult> Execute(ExecuteContext<GeneratePictureWorkflowPathsArguments> executeContext)
        {
            try
            {
                var workingDirectory = Path.Combine(executeContext.Arguments.TemporaryRootDirectory, Guid.NewGuid().ToString());

                var sourceFilePath = Path.Combine(
                    workingDirectory, Guid.NewGuid().ToString() + Path.GetExtension(executeContext.Arguments.SourceKey));
                var smallPictureFilePath = Path.Combine(
                    workingDirectory, Guid.NewGuid().ToString() + ResizedPictureFileExtension);
                var mediumPictureFilePath = Path.Combine(
                    workingDirectory, Guid.NewGuid().ToString() + ResizedPictureFileExtension);
                var largePictureFilePath = Path.Combine(
                    workingDirectory, Guid.NewGuid().ToString() + ResizedPictureFileExtension);

                Directory.CreateDirectory(workingDirectory);

                logger.LogDebug("Generated picture workflow paths in {WorkingDirectory}",
                    workingDirectory);
                return executeContext.CompletedWithVariables(new Dictionary<string, object>
                {
                    [RoutingSlipVariableNames.Workflow.TemporalDirectory] = workingDirectory,
                    [RoutingSlipVariableNames.Picture.OriginalFilePath] = sourceFilePath,
                    [RoutingSlipVariableNames.Picture.SmallResizedFilePath] = smallPictureFilePath,
                    [RoutingSlipVariableNames.Picture.MediumResizedFilePath] = mediumPictureFilePath,
                    [RoutingSlipVariableNames.Picture.LargeResizedFilePath] = largePictureFilePath
                } );
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to generate picture workflow paths for {SourceKey}",
                    executeContext.Arguments.SourceKey);
                throw;
            }
        }
    }
}