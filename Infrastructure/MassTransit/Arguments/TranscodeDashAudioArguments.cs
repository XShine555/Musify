namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record TranscodeDashAudioArguments(
        Guid TrackId,
        string SourceFilePathVariable,
        string WorkingDirectoryVariable);
}