using FluentValidation;
using Musify.Api.DataTransferObjects.PlayLists;

namespace Musify.Api.Validators.PlayLists
{
    public sealed class UpdatePlayListPictureRequestValidator : AbstractValidator<UpdatePlayListPictureRequest>
    {
        public UpdatePlayListPictureRequestValidator()
        {
            RuleFor(x => x.PictureIntentId)
                .NotEmpty();
        }
    }
}
