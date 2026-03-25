namespace Musify.Infrastructure.MassTransit.RoutingSlip
{
    internal static class ActivityNames
    {
        internal const string GenerateAudioWorkflowPaths = "GenerateAudioWorkflowPaths";
        internal const string GeneratePictureWorkflowPaths = "GeneratePictureWorkflowPaths";
        internal const string DownloadFile = "DownloadFile";
        internal const string TranscodeAudio = "TranscodeAudio";
        internal const string ResizeSmall = "ResizeSmall";
        internal const string ResizeMedium = "ResizeMedium";
        internal const string ResizeLarge = "ResizeLarge";
        internal const string UploadFiles = "UploadFiles";
        internal const string RemoveFile = "RemoveFile";
        internal const string DeleteOriginal = "DeleteOriginal";
        internal const string UpdateTrackPicture = "UpdateTrackPicture";
        internal const string UpdatePlayListPicture = "UpdatePlayListPicture";
        internal const string UpdateTrackAudio = "UpdateTrackAudio";
    }
}
