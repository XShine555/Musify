namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record GenerateAudioWorkflowPathsArguments(
        Guid TrackId,
        string TemporaryRootDirectory,
        string SourceKey);
}
