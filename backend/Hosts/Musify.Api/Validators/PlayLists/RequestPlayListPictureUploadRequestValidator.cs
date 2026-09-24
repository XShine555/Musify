using FluentValidation;
using Musify.Api.DataTransferObjects.PlayLists;
using Musify.Application.Configuration;

namespace Musify.Api.Validators.PlayLists;

public sealed class RequestPlayListPictureUploadRequestValidator : AbstractValidator<RequestPlayListPictureUploadRequest>
{
    public RequestPlayListPictureUploadRequestValidator(UploadIntentConfiguration uploadConfiguration)
    {
        RuleFor(x => x.FileType)
            .MustBeFileType(Uploads.PictureFileTypes);

        RuleFor(x => x.ContentType)
            .MustBeContentType(Uploads.PictureContentTypes);

        RuleFor(x => x.ExpectedSizeBytes)
            .MustBeValidUploadSize(uploadConfiguration.MaxUploadBytes);
    }
}
