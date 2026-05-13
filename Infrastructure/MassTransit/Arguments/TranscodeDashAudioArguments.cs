namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record TranscodeDashAudioArguments(
        Guid TrackId,
        string SourceFilePathVariable,
        string WorkingDirectoryVariable);
}