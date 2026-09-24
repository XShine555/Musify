namespace Musify.Infrastructure.MassTransit.Arguments;

public record ResizePictureLocalArguments(
    string SourceFilePathVariable,
    string DestinationFilePathVariable,
    int Width,
    int Height);
