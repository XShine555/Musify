using ErrorOr;
using Mediator;
using Musify.Application.Configuration;
using Musify.Application.Services;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks
{
    public record AudioUploadSpec(string FileType, string ContentType, long? ExpectedSizeBytes = null);

    public record RequestTrackUploadUrlsCommand(
        long UserId,
        string? PictureFileType,
        string? PictureContentType,
        string? AudioFileType = null,
        string? AudioContentType = null,
        long? ExpectedPictureSizeBytes = null,
        long? ExpectedAudioSizeBytes = null,
        IReadOnlyList<AudioUploadSpec>? Audios = null)
        : ICommand<ErrorOr<TrackUploadUrlsResponse>>;

    public class RequestTrackUploadUrlsCommandHandler(
        UploadIntentIssuer issuer,
        ApplicationStorageConfiguration storageConfiguration,
        TrackConfiguration trackConfiguration,
        UploadIntentConfiguration uploadIntentConfiguration)
        : ICommandHandler<RequestTrackUploadUrlsCommand, ErrorOr<TrackUploadUrlsResponse>>
    {
        private const int PictureKeyGroup = 1;

        public async ValueTask<ErrorOr<TrackUploadUrlsResponse>> Handle(RequestTrackUploadUrlsCommand request, CancellationToken cancellationToken)
        {
            var audios = ResolveAudios(request);
            if (audios.Count == 0)
                return Error.Validation(description: "At least one audio file is required.");

            var hasPicture = request.PictureFileType != null && request.PictureContentType != null;
            var routes = trackConfiguration.Routes;

            var uploads = new List<UploadRequest>();
            if (hasPicture)
            {
                for (var index = 0; index < audios.Count; index++)
                {
                    uploads.Add(new UploadRequest(
                        UploadIntentPurpose.TrackPicture, routes, request.PictureFileType!, request.PictureContentType!,
                        request.ExpectedPictureSizeBytes, PictureKeyGroup));
                }
            }

            uploads.AddRange(audios.Select(audio =>
                new UploadRequest(UploadIntentPurpose.TrackAudio, routes, audio.FileType, audio.ContentType, audio.ExpectedSizeBytes)));

            var issued = await issuer.IssueAsync(request.UserId, uploads, cancellationToken);
            if (issued.IsError)
                return issued.Errors;

            var pictures = hasPicture ? issued.Value.Take(audios.Count).ToList() : [];
            var audioUploads = issued.Value.Skip(pictures.Count).ToList();

            var items = audioUploads
                .Select((audio, index) => new TrackAudioUpload(
                    audio.IntentId,
                    hasPicture ? pictures[index].IntentId : null,
                    audio.Key,
                    audio.ObjectName,
                    audio.ContentType,
                    audio.UploadUrl))
                .ToList();

            var firstPicture = pictures.Count > 0 ? pictures[0] : null;
            var firstAudio = audioUploads[0];

            return new TrackUploadUrlsResponse(
                firstPicture?.IntentId,
                firstAudio.IntentId,
                storageConfiguration.Bucket,
                firstPicture?.Key,
                firstPicture?.ObjectName,
                firstPicture?.ContentType,
                firstPicture?.UploadUrl,
                firstAudio.Key,
                firstAudio.ObjectName,
                firstAudio.ContentType,
                firstAudio.UploadUrl,
                uploadIntentConfiguration.UploadUrlExpiresInSeconds,
                items);
        }

        private static IReadOnlyList<AudioUploadSpec> ResolveAudios(RequestTrackUploadUrlsCommand request)
        {
            if (request.Audios is { Count: > 0 } audios)
                return audios;

            return request.AudioFileType != null && request.AudioContentType != null
                ? [new AudioUploadSpec(request.AudioFileType, request.AudioContentType, request.ExpectedAudioSizeBytes)]
                : [];
        }
    }
}
