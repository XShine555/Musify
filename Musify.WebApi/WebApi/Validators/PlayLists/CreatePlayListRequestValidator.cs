using FluentValidation;
using WebApi.DataTransferObjects.PlayLists;

namespace WebApi.Validators.PlayLists;

public sealed class CreatePlayListRequestValidator : AbstractValidator<CreatePlayListRequest>
{
    public CreatePlayListRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(256);
    }
}
