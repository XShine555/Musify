namespace Musify.Infrastructure.Messaging.Activities.Arguments
{
    public record GeneratePictureWorkflowPathsArguments(
        string TemporaryRootDirectory,
        string SourceKeyName,
        string SmallPictureKeyName,
        string MediumPictureKeyName,
        string LargePictureKeyName);
}
