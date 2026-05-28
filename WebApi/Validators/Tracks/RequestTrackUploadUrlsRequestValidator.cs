using FluentValidation;
using WebApi.DataTransferObjects.Tracks;

namespace WebApi.Validators.Tracks;

public sealed class RequestTrackUploadUrlsRequestValidator : AbstractValidator<RequestTrackUploadUrlsRequest>
{
    public RequestTrackUploadUrlsRequestValidator()
    {
        RuleFor(x => x.PictureFileType)
            .NotEmpty();

        RuleFor(x => x.PictureContentType)
            .NotEmpty();

        RuleFor(x => x.AudioFileType)
            .NotEmpty();

        RuleFor(x => x.AudioContentType)
            .NotEmpty();

        RuleFor(x => x.ExpectedPictureSizeBytes)
            .GreaterThan(0)
            .When(x => x.ExpectedPictureSizeBytes.HasValue);

        RuleFor(x => x.ExpectedAudioSizeBytes)
            .GreaterThan(0)
            .When(x => x.ExpectedAudioSizeBytes.HasValue);
    }
}
