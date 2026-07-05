using FluentValidation;
using Musify.Api.DataTransferObjects.Users;

namespace Musify.Api.Validators.Users;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(48);

        RuleFor(x => x.FirstName)
            .MaximumLength(48)
            .When(x => x.FirstName is not null);

        RuleFor(x => x.SecondName)
            .MaximumLength(48)
            .When(x => x.SecondName is not null);
    }
}
