using FluentValidation;
using Musify.Api.DataTransferObjects.Albums;

namespace Musify.Api.Validators.Albums;

public sealed class CreateAlbumRequestValidator : AbstractValidator<CreateAlbumRequest>
{
    public CreateAlbumRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(Limits.AlbumTitle);

        RuleFor(x => x.Description)
            .MaximumLength(Limits.Description);

        RuleFor(x => x.ReleaseYear)
            .InclusiveBetween(Limits.EarliestReleaseYear, Limits.LatestReleaseYear)
            .When(x => x.ReleaseYear.HasValue);

        RuleFor(x => x.PictureIntentId)
            .NotEmpty();
    }
}
