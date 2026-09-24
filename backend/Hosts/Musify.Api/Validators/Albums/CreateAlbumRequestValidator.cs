using FluentValidation;
using Musify.Api.DataTransferObjects.Albums;

namespace Musify.Api.Validators.Albums
{
    public sealed class CreateAlbumRequestValidator : AbstractValidator<CreateAlbumRequest>
    {
        public const int MaxTracksOnCreate = 500;

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

            RuleFor(x => x.Type)
                .IsInEnum();

            RuleFor(x => x.TrackIds)
                .Must(ids => ids!.Count <= MaxTracksOnCreate)
                .WithMessage($"At most {MaxTracksOnCreate} tracks can be added at once.")
                .When(x => x.TrackIds != null);

            RuleForEach(x => x.TrackIds)
                .NotEmpty()
                .When(x => x.TrackIds != null);
        }
    }
}
