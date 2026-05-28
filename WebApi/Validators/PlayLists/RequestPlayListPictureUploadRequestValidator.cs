using FluentValidation;
using WebApi.DataTransferObjects.PlayLists;

namespace WebApi.Validators.PlayLists;

public sealed class RequestPlayListPictureUploadRequestValidator : AbstractValidator<RequestPlayListPictureUploadRequest>
{
    public RequestPlayListPictureUploadRequestValidator()
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
