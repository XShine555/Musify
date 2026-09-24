using FluentValidation;
using Musify.Api.DataTransferObjects.Tracks;

namespace Musify.Api.Validators.Tracks
{
    public sealed class SetTracksPublishedRequestValidator : AbstractValidator<SetTracksPublishedRequest>
    {
        public const int MaxTracksPerRequest = 100;

        public SetTracksPublishedRequestValidator()
        {
            RuleFor(x => x.TrackIds)
                .NotEmpty()
                .Must(ids => ids.Count <= MaxTracksPerRequest)
                .WithMessage($"At most {MaxTracksPerRequest} tracks can be updated at once.");

            RuleForEach(x => x.TrackIds)
                .NotEmpty();
        }
    }
}
