namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record ResizePictureLocalArguments(
        string SourceFilePathVariable,
        string DestinationFilePathVariable,
        int Width,
        int Height);
}
