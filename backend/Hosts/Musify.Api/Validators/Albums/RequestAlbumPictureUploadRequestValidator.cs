using FluentValidation;
using Musify.Api.DataTransferObjects.Albums;
using Musify.Application.Configuration;

namespace Musify.Api.Validators.Albums;

public sealed class RequestAlbumPictureUploadRequestValidator : AbstractValidator<RequestAlbumPictureUploadRequest>
{
    public RequestAlbumPictureUploadRequestValidator(UploadIntentConfiguration uploadConfiguration)
    {
        RuleFor(x => x.FileType)
            .MustBeFileType(Uploads.PictureFileTypes);

        RuleFor(x => x.ContentType)
            .MustBeContentType(Uploads.PictureContentTypes);

        RuleFor(x => x.ExpectedSizeBytes)
            .MustBeValidUploadSize(uploadConfiguration.MaxUploadBytes);
    }
}
