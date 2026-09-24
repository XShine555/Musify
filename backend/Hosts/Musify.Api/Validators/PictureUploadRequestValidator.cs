using FluentValidation;
using Musify.Api.DataTransferObjects;
using Musify.Application.Configuration;

namespace Musify.Api.Validators;

public sealed class PictureUploadRequestValidator : AbstractValidator<PictureUploadRequest>
{
    public PictureUploadRequestValidator(UploadIntentConfiguration uploadConfiguration)
    {
        RuleFor(x => x.FileType)
            .MustBeFileType(Uploads.PictureFileTypes);

        RuleFor(x => x.ContentType)
            .MustBeContentType(Uploads.PictureContentTypes);

        RuleFor(x => x.ExpectedSizeBytes)
            .MustBeValidUploadSize(uploadConfiguration.MaxUploadBytes);
    }
}
