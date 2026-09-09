namespace Musify.Infrastructure.MassTransit.RoutingSlip
{
    internal static class ActivityNames
    {
        internal const string MarkTrackAsRemoving = "MarkTrackAsRemoving";
        internal const string MarkTrackAsFailed = "MarkTrackAsFailed";
        internal const string DeleteTrackFromDb = "DeleteTrackFromDb";
        internal const string MarkPlayListAsRemoving = "MarkPlayListAsRemoving";
        internal const string MarkPlayListAsFailed = "MarkPlayListAsFailed";
        internal const string DeletePlayListFromDb = "DeletePlayListFromDb";

        internal const string GenerateAudioWorkflowPaths = "GenerateAudioWorkflowPaths";
        internal const string GeneratePictureWorkflowPaths = "GeneratePictureWorkflowPaths";

        internal const string DownloadFile = "DownloadFile";
        internal const string DownloadYouTubeAudio = "DownloadYouTubeAudio";
        internal const string DownloadThumbnail = "DownloadThumbnail";

        internal const string TranscodeAudio = "TranscodeAudio";

        internal const string ResizeSmall = "ResizeSmall";
        internal const string ResizeMedium = "ResizeMedium";
        internal const string ResizeLarge = "ResizeLarge";

        internal const string UploadSmall = "UploadSmall";
        internal const string UploadMedium = "UploadMedium";
        internal const string UploadLarge = "UploadLarge";

        internal const string TransferFiles = "TransferFiles";

        internal const string UpdateTrackPicture = "UpdateTrackPicture";
        internal const string UpdatePlayListPicture = "UpdatePlayListPicture";
        internal const string UpdateTrackAudio = "UpdateTrackAudio";

        internal const string CopyPictureToFinal = "CopyPictureToFinal";
        internal const string CopyAudioToFinal = "CopyAudioToFinal";

        internal const string ConsumeUploadIntents = "ConsumeUploadIntents";

        internal const string PublishTrackProcessingEvents = "PublishTrackProcessingEvents";
        internal const string PublishPlayListPictureProcessingEvent = "PublishPlayListPictureProcessingEvent";

        internal const string RemoveTrackOriginalPicture = "RemoveTrackOriginalPicture";
        internal const string RemoveTrackSmallPicture = "RemoveTrackSmallPicture";
        internal const string RemoveTrackMediumPicture = "RemoveTrackMediumPicture";
        internal const string RemoveTrackLargePicture = "RemoveTrackLargePicture";
        internal const string RemoveTrackOriginalAudio = "RemoveTrackOriginalAudio";
        internal const string RemoveTrackProcessedAudio = "RemoveTrackProcessedAudio";

        internal const string RemovePlayListOriginalPicture = "RemovePlayListOriginalPicture";
        internal const string RemovePlayListSmallPicture = "RemovePlayListSmallPicture";
        internal const string RemovePlayListMediumPicture = "RemovePlayListMediumPicture";
        internal const string RemovePlayListLargePicture = "RemovePlayListLargePicture";
    }
}
