using FluentValidation;
using Musify.Api.DataTransferObjects.Albums;

namespace Musify.Api.Validators.Albums
{
    public sealed class UpdateAlbumPictureRequestValidator : AbstractValidator<UpdateAlbumPictureRequest>
    {
        public UpdateAlbumPictureRequestValidator()
        {
            RuleFor(x => x.PictureIntentId)
                .NotEmpty();
        }
    }
}
