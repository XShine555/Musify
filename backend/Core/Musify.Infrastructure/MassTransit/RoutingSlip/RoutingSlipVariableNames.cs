namespace Musify.Infrastructure.MassTransit;

internal static class RoutingSlipVariableNames
{
    internal static class Workflow
    {
        internal const string TemporalDirectory = "Workflow.TemporalDirectory";
        internal const string CorrelationId = "Workflow.CorrelationId";
        internal const string SubjectId = "Workflow.SubjectId";
        internal const string ProcessKind = "Workflow.ProcessKind";
        internal const string Bucket = "Workflow.Bucket";
        internal const string PictureKey = "Workflow.PictureKey";
        internal const string AudioKey = "Workflow.AudioKey";
    }

    internal static class ProcessKinds
    {
        internal const string TrackPicture = "TrackPicture";
        internal const string TrackAudio = "TrackAudio";
        internal const string PlayListPicture = "PlayListPicture";
        internal const string AlbumPicture = "AlbumPicture";
        internal const string TrackCreation = "TrackCreation";
        internal const string AlbumCreation = "AlbumCreation";
        internal const string PlayListCreation = "PlayListCreation";
    }

    internal static class Audio
    {
        internal const string SourceFilePath = "Audio.SourceFilePath";
        internal const string TranscodedDirectory = "Audio.TranscodedDirectory";
        internal const string DurationSeconds = "Audio.DurationSeconds";
    }

    internal static class Picture
    {
        internal const string OriginalFilePath = "Picture.OriginalFilePath";
        internal const string SmallResizedFilePath = "Picture.SmallResizedFilePath";
        internal const string MediumResizedFilePath = "Picture.MediumResizedFilePath";
        internal const string LargeResizedFilePath = "Picture.LargeResizedFilePath";
    }
}
