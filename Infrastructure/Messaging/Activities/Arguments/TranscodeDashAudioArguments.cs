namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record TranscodeDashAudioArguments(string SourceFilePath, string DestinationFolderName);
}