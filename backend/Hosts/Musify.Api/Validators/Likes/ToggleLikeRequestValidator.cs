using FluentValidation;
using Musify.Api.DataTransferObjects.Likes;
using Musify.Domain.ValueObjects;

namespace Musify.Api.Validators.Likes;

public sealed class ToggleLikeRequestValidator : AbstractValidator<ToggleLikeRequest>
{
    public ToggleLikeRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.TrackId.HasValue ^ (x.Source.HasValue && !string.IsNullOrEmpty(x.ExternalId)))
            .WithMessage("Provide either trackId, or source and externalId, but not both.");

        When(x => x.Source.HasValue, () =>
        {
            RuleFor(x => x.Source)
                .Must(source => source == TrackSource.YouTube)
                .WithMessage("Unsupported source.");

            RuleFor(x => x.ExternalId)
                .NotEmpty()
                .Matches("^[A-Za-z0-9_-]{11}$")
                .WithMessage("externalId must be a valid YouTube video id.");
        });
    }
}
