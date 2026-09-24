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
                .MaximumLength(Limits.PlayListName)
                .When(x => x.Name != null);

            RuleFor(x => x.Description)
                .MaximumLength(Limits.Description)
                .When(x => x.Description != null);

            RuleFor(x => x.Visibility)
                .IsInEnum()
                .When(x => x.Visibility != null);
        }
    }
}
