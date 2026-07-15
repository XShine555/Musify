using FluentValidation;
using Musify.Api.DataTransferObjects.PlayLists;

namespace Musify.Api.Validators.PlayLists;

public sealed class AddYouTubeTrackRequestValidator : AbstractValidator<AddYouTubeTrackRequest>
{
    public AddYouTubeTrackRequestValidator()
    {
        RuleFor(x => x.VideoId)
            .NotEmpty()
            .Matches("^[A-Za-z0-9_-]{11}$");

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Artist)
            .MaximumLength(400);

        RuleFor(x => x.DurationSeconds)
            .GreaterThan(0);

        RuleFor(x => x.ThumbnailUrl)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .WithMessage("ThumbnailUrl must be an absolute https URL.");
    }
}
