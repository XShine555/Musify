using FluentValidation;
using Musify.Api.DataTransferObjects.Tracks;
using Musify.Domain.ValueObjects;

namespace Musify.Api.Validators.Tracks;

public sealed class CreateTrackRequestValidator : AbstractValidator<CreateTrackRequest>
{
    public CreateTrackRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(Limits.TrackTitle);

        RuleFor(x => x.PictureIntentId)
            .NotEmpty();

        RuleFor(x => x.AudioIntentId)
            .NotEmpty();

        RuleFor(x => x.Tags)
            .NotEmpty()
            .WithMessage("At least one tag is required.");

        RuleFor(x => x.Tags)
            .Must(tags => GenreCompatibility.FindConflicts(tags).Count == 0)
            .When(x => x.Tags is { Count: > 0 })
            .WithMessage(x =>
            {
                var conflict = GenreCompatibility.FindConflicts(x.Tags).First();
                return $"Tags '{conflict.First}' and '{conflict.Second}' are not compatible.";
            });
    }
}
