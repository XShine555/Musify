using Ardalis.Result;
using Mediator;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Commands
{
    public record RequestTrackUploadUrlsCommand(
        long UserId,
        string PictureFileType,
        string PictureContentType,
        string AudioFileType,
        string AudioContentType,
        long? ExpectedPictureSizeBytes = null,
        long? ExpectedAudioSizeBytes = null)
        : ICommand<Result<TrackUploadUrlsResponse>>;
}
