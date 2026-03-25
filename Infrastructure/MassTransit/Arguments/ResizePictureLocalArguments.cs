namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record ResizePictureLocalArguments(
        string SourceFilePathVariableName,
        string DestinationFilePathVariableName,
        int Width,
        int Height);
}
