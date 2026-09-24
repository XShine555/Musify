using FluentValidation;
using Musify.Api.DataTransferObjects.Tracks;

namespace Musify.Api.Validators.Tracks
{
    public sealed class DeleteTracksRequestValidator : AbstractValidator<DeleteTracksRequest>
    {
        public const int MaxTracksPerRequest = 100;

        public DeleteTracksRequestValidator()
        {
            RuleFor(x => x.TrackIds)
                .NotEmpty()
                .Must(ids => ids.Count <= MaxTracksPerRequest)
                .WithMessage($"At most {MaxTracksPerRequest} tracks can be deleted at once.");

            RuleForEach(x => x.TrackIds)
                .NotEmpty();
        }
    }
}
