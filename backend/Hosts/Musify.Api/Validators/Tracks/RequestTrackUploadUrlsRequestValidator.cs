using FluentValidation;
using Musify.Api.DataTransferObjects.Tracks;
using Musify.Application.Configuration;

namespace Musify.Api.Validators.Tracks
{
    public sealed class RequestTrackUploadUrlsRequestValidator : AbstractValidator<RequestTrackUploadUrlsRequest>
    {
        public const int MaxAudiosPerRequest = 20;

        public RequestTrackUploadUrlsRequestValidator(UploadIntentConfiguration uploadConfiguration)
        {
            When(x => x.PictureFileType != null || x.PictureContentType != null, () =>
            {
                RuleFor(x => x.PictureFileType!)
                    .MustBeFileType(Uploads.PictureFileTypes);

                RuleFor(x => x.PictureContentType!)
                    .MustBeContentType(Uploads.PictureContentTypes);

                RuleFor(x => x.ExpectedPictureSizeBytes)
                    .MustBeValidUploadSize(uploadConfiguration.MaxUploadBytes);
            });

            When(x => x.Audios is { Count: > 0 }, () =>
            {
                RuleFor(x => x.AudioFileType)
                    .Null()
                    .WithMessage("Use either the audio fields or the audios list, not both.");

                RuleFor(x => x.AudioContentType)
                    .Null()
                    .WithMessage("Use either the audio fields or the audios list, not both.");

                RuleFor(x => x.Audios!)
                    .Must(audios => audios.Count <= MaxAudiosPerRequest)
                    .WithMessage($"At most {MaxAudiosPerRequest} audio files can be requested at once.");

                RuleForEach(x => x.Audios!).ChildRules(audio =>
                {
                    audio.RuleFor(a => a.FileType).MustBeFileType(Uploads.AudioFileTypes);
                    audio.RuleFor(a => a.ContentType).MustBeContentType(Uploads.AudioContentTypes);
                    audio.RuleFor(a => a.ExpectedSizeBytes).MustBeValidUploadSize(uploadConfiguration.MaxUploadBytes);
                });
            }).Otherwise(() =>
            {
                RuleFor(x => x.AudioFileType!)
                    .MustBeFileType(Uploads.AudioFileTypes);

                RuleFor(x => x.AudioContentType!)
                    .MustBeContentType(Uploads.AudioContentTypes);

                RuleFor(x => x.ExpectedAudioSizeBytes)
                    .MustBeValidUploadSize(uploadConfiguration.MaxUploadBytes);
            });
        }
    }
}
