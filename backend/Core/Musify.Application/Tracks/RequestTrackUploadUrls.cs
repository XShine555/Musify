using ErrorOr;
using Mediator;
using Musify.Application.Configuration;
using Musify.Application.Services;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks
{
    public record RequestTrackUploadUrlsCommand(
        long UserId,
        string PictureFileType,
        string PictureContentType,
        string AudioFileType,
        string AudioContentType,
        long? ExpectedPictureSizeBytes = null,
        long? ExpectedAudioSizeBytes = null)
        : ICommand<ErrorOr<TrackUploadUrlsResponse>>;

    public class RequestTrackUploadUrlsCommandHandler(
        UploadIntentIssuer issuer,
        ApplicationStorageConfiguration storageConfiguration,
        TrackConfiguration trackConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<RequestTrackUploadUrlsCommand, ErrorOr<TrackUploadUrlsResponse>>
    {
        public async ValueTask<ErrorOr<TrackUploadUrlsResponse>> Handle(RequestTrackUploadUrlsCommand request, CancellationToken cancellationToken)
        {
            var routes = trackConfiguration.Routes;
            var issued = await issuer.IssueAsync(
                request.UserId,
                [
                    new UploadRequest(UploadIntentPurpose.TrackPicture, routes, request.PictureFileType, request.PictureContentType, request.ExpectedPictureSizeBytes),
                    new UploadRequest(UploadIntentPurpose.TrackAudio, routes, request.AudioFileType, request.AudioContentType, request.ExpectedAudioSizeBytes)
                ],
                cancellationToken);
            if (issued.IsError)
                return issued.Errors;

            var picture = issued.Value[0];
            var audio = issued.Value[1];

            return new TrackUploadUrlsResponse(
                picture.IntentId,
                audio.IntentId,
                storageConfiguration.Bucket,
                picture.Key,
                picture.ObjectName,
                picture.ContentType,
                picture.UploadUrl,
                audio.Key,
                audio.ObjectName,
                audio.ContentType,
                audio.UploadUrl,
                uploadIntentConfiguration.UploadUrlExpiresInSeconds);
        }
    }
}
