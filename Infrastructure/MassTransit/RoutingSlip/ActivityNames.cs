namespace Musify.Infrastructure.MassTransit.RoutingSlip
{
    internal static class ActivityNames
    {
        internal const string DeleteTrack = "DeleteTrack";
        internal const string MarkTrackAsRemoving = "MarkTrackAsRemoving";
        internal const string DeleteTrackFromDb = "DeleteTrackFromDb";
        internal const string MarkPlayListAsRemoving = "MarkPlayListAsRemoving";
        internal const string DeletePlayListFromDb = "DeletePlayListFromDb";
        internal const string GenerateAudioWorkflowPaths = "GenerateAudioWorkflowPaths";
        internal const string GeneratePictureWorkflowPaths = "GeneratePictureWorkflowPaths";
        internal const string DownloadFile = "DownloadFile";
        internal const string TranscodeAudio = "TranscodeAudio";
        internal const string ResizeSmall = "ResizeSmall";
        internal const string ResizeMedium = "ResizeMedium";
        internal const string ResizeLarge = "ResizeLarge";
        internal const string UploadSmall = "UploadSmall";
        internal const string UploadMedium = "UploadMedium";
        internal const string UploadLarge = "UploadLarge";
        internal const string TransferFiles = "TransferFiles";
        internal const string RemoveFile = "RemoveFile";
        internal const string DeleteOriginal = "DeleteOriginal";
        internal const string UpdateTrackPicture = "UpdateTrackPicture";
        internal const string UpdatePlayListPicture = "UpdatePlayListPicture";
        internal const string UpdateTrackAudio = "UpdateTrackAudio";
    }
}
