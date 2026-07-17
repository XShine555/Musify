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
    }
}
