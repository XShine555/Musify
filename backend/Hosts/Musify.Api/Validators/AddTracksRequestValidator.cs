using FluentValidation;
using Musify.Api.DataTransferObjects;

namespace Musify.Api.Validators;

public sealed class AddTracksRequestValidator : AbstractValidator<AddTracksRequest>
{
    public const int MaxTracksPerRequest = 500;

    public AddTracksRequestValidator()
    {
        RuleFor(x => x.TrackIds)
            .NotEmpty()
            .Must(ids => ids.Count <= MaxTracksPerRequest)
            .WithMessage($"At most {MaxTracksPerRequest} tracks can be added at once.");

        RuleForEach(x => x.TrackIds)
            .NotEmpty();
    }
}
