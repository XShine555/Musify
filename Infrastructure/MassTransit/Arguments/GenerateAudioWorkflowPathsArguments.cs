namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record GenerateAudioWorkflowPathsArguments(
        string TemporaryRootDirectory,
        string SourceKeyName);
}
