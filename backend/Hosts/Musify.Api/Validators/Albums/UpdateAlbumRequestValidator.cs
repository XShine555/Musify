using FluentValidation;
using Musify.Api.DataTransferObjects.Albums;

namespace Musify.Api.Validators.Albums;

public sealed class UpdateAlbumRequestValidator : AbstractValidator<UpdateAlbumRequest>
{
    public UpdateAlbumRequestValidator()
    {
        RuleFor(x => x.NewTitle)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.NewDescription)
            .MaximumLength(256);

        RuleFor(x => x.NewReleaseYear)
            .InclusiveBetween(AlbumReleaseYear.Earliest, AlbumReleaseYear.Latest)
            .When(x => x.NewReleaseYear.HasValue);
    }
}
