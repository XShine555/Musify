namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record TranscodeDashAudioArguments(
        string SourceFilePathVariableName,
        string WorkingDirectoryVariableName);
}