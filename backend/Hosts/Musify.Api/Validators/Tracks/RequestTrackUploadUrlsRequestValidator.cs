using FluentValidation;
using Musify.Api.DataTransferObjects.Tracks;
using Musify.Application.Configuration;

namespace Musify.Api.Validators.Tracks;

public sealed class RequestTrackUploadUrlsRequestValidator : AbstractValidator<RequestTrackUploadUrlsRequest>
{
    public RequestTrackUploadUrlsRequestValidator(UploadIntentConfiguration uploadConfiguration)
    {
        RuleFor(x => x.PictureFileType)
            .MustBeFileType(Uploads.PictureFileTypes);

        RuleFor(x => x.PictureContentType)
            .MustBeContentType(Uploads.PictureContentTypes);

        RuleFor(x => x.AudioFileType)
            .MustBeFileType(Uploads.AudioFileTypes);

        RuleFor(x => x.AudioContentType)
            .MustBeContentType(Uploads.AudioContentTypes);

        RuleFor(x => x.ExpectedPictureSizeBytes)
            .MustBeValidUploadSize(uploadConfiguration.MaxUploadBytes);

        RuleFor(x => x.ExpectedAudioSizeBytes)
            .MustBeValidUploadSize(uploadConfiguration.MaxUploadBytes);
    }
}
