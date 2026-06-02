namespace Musify.Infrastructure.MassTransit.Arguments
{
    public record TranscodeAudioArguments(
        Guid TrackId,
        string SourceFilePathVariable,
        string WorkingDirectoryVariable);
}
