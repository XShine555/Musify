namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record ResizePictureLocalArguments(
        string SourceFilePath,
        string DestinationFilePath,
        int Width,
        int Height);
}
