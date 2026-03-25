namespace Musify.Infrastructure.MassTransit.Activities.Arguments
{
    public record GeneratePictureWorkflowPathsArguments(
        string TemporaryRootDirectory,
        string SourceKeyName,
        string SmallPictureKeyName,
        string MediumPictureKeyName,
        string LargePictureKeyName);
}
