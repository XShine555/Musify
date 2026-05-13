namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record GenerateAudioWorkflowPathsArguments(
        Guid TrackId,
        string TemporaryRootDirectory,
        string SourceKey);
}
