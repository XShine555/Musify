using FluentValidation;
using Musify.Api.DataTransferObjects.Albums;

namespace Musify.Api.Validators.Albums;

public sealed class RequestAlbumPictureUploadRequestValidator : AbstractValidator<RequestAlbumPictureUploadRequest>
{
    public RequestAlbumPictureUploadRequestValidator()
    {
        RuleFor(x => x.FileType)
            .NotEmpty();

        RuleFor(x => x.ContentType)
            .NotEmpty();

        RuleFor(x => x.ExpectedSizeBytes)
            .GreaterThan(0)
            .When(x => x.ExpectedSizeBytes.HasValue);
    }
}
