namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record GeneratePictureWorkflowPathsArguments(
        string TemporaryRootDirectory,
        string SourceKey);
}