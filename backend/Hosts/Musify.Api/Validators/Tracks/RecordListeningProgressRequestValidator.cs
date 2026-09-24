using FluentValidation;
using Musify.Api.DataTransferObjects.Tracks;

namespace Musify.Api.Validators.Tracks
{
    public sealed class RecordListeningProgressRequestValidator : AbstractValidator<RecordListeningProgressRequest>
    {
        public RecordListeningProgressRequestValidator()
        {
            RuleFor(x => x.PlayedSeconds)
                .Must(seconds => double.IsFinite(seconds) && seconds >= 0)
                .WithMessage("'Played Seconds' must be a non-negative number.");
        }
    }
}
