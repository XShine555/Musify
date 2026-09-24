using FluentValidation;
using Musify.Api.DataTransferObjects.PlayLists;

namespace Musify.Api.Validators.PlayLists
{
    public sealed class UpdatePlayListRequestValidator : AbstractValidator<UpdatePlayListRequest>
    {
        public UpdatePlayListRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(Limits.PlayListName);

            RuleFor(x => x.Description)
                .MaximumLength(Limits.Description);

            RuleFor(x => x.Visibility)
                .NotNull()
                .IsInEnum();
        }
    }
}
