using FluentValidation;
using WebApi.DataTransferObjects.PlayLists;

namespace WebApi.Validators.PlayLists;

public sealed class UpdatePlayListRequestValidator : AbstractValidator<UpdatePlayListRequest>
{
    public UpdatePlayListRequestValidator()
    {
        RuleFor(x => x.NewName)
            .MaximumLength(50)
            .When(x => x.NewName is not null);

        RuleFor(x => x.NewDescription)
            .MaximumLength(256)
            .When(x => x.NewDescription is not null);
    }
}
