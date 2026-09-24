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
            .When(x => x.NewName != null);

        RuleFor(x => x.NewDescription)
            .MaximumLength(256)
            .When(x => x.NewDescription != null);

        RuleFor(x => x.NewVisibility)
            .IsInEnum()
            .When(x => x.NewVisibility != null);
    }
}
