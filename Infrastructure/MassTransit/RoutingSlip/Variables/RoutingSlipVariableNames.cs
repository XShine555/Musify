namespace Musify.Infrastructure.Messaging
{
    internal static class RoutingSlipVariableNames
    {
        internal static class Workflow
        {
            internal const string TempDirectory = "Workflow.TempDirectory";
            internal const string CorrelationId = "Workflow.CorrelationId";
        }

        internal static class Audio
        {
            internal const string SourceFilePath = "Audio.SourceFilePath";
            internal const string WorkingDirectory = "Audio.WorkingDirectory";
            internal const string TranscodedDirectory = "Audio.TranscodedDirectory";
        }

        internal static class Picture
        {
            internal const string OriginalFilePath = "Picture.OriginalFilePath";
            internal const string SmallResizedFilePath = "Picture.SmallResizedFilePath";
            internal const string MediumResizedFilePath = "Picture.MediumResizedFilePath";
            internal const string LargeResizedFilePath = "Picture.LargeResizedFilePath";
            internal const string DestinationFolderName = "Picture.DestinationFolderName";
        }
    }
}
