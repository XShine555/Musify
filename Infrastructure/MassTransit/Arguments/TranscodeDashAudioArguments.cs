namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record TranscodeDashAudioArguments(
        string SourceFilePathVariable,
        string WorkingDirectoryVariable);
}