using FluentValidation;
using Musify.Api.DataTransferObjects.Albums;

namespace Musify.Api.Validators.Albums;

public sealed class CreateAlbumRequestValidator : AbstractValidator<CreateAlbumRequest>
{
    public CreateAlbumRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(256);

        RuleFor(x => x.ReleaseYear)
            .InclusiveBetween(AlbumReleaseYear.Earliest, AlbumReleaseYear.Latest)
            .When(x => x.ReleaseYear.HasValue);
    }
}
