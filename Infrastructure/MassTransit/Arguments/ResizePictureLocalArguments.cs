namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record ResizePictureLocalArguments(
        string SourceFilePathVariableName,
        string DestinationFilePathVariableName,
        int Width,
        int Height);
}
