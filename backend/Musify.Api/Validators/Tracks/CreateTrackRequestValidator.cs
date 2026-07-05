using FluentValidation;
using Musify.Api.DataTransferObjects.Tracks;

namespace Musify.Api.Validators.Tracks;

public sealed class CreateTrackRequestValidator : AbstractValidator<CreateTrackRequest>
{
    public CreateTrackRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.PictureIntentId)
            .NotEmpty();

        RuleFor(x => x.AudioIntentId)
            .NotEmpty();
    }
}
