namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record GenerateAudioWorkflowPathsArguments(
        string TemporaryRootDirectory,
        string SourceKeyName);
}
