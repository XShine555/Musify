using FluentValidation;
using Musify.Api.DataTransferObjects.PlayLists;

namespace Musify.Api.Validators.PlayLists;

public sealed class UpdatePlayListRequestValidator : AbstractValidator<UpdatePlayListRequest>
{
    public UpdatePlayListRequestValidator()
    {
        RuleFor(x => x.NewName)
            .NotEmpty()
            .MaximumLength(50)
            .When(x => x.NewName is not null);

        RuleFor(x => x.NewDescription)
            .NotEmpty()
            .MaximumLength(256)
            .When(x => x.NewDescription is not null);
    }
}
