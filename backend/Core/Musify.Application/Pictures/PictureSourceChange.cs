using ErrorOr;
using Musify.Application.Configuration;
using Musify.Application.Services;
using Musify.Application.Shared;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Pictures
{
    /// <summary>What an album or playlist needs to switch to a freshly uploaded picture.</summary>
    public sealed record PreparedPicture(UploadIntent Intent, EntityPictures Pictures, string FinalKey, ImageSizes Sizes);

    public static class PictureSourceChange
    {
        public static async Task<ErrorOr<PreparedPicture>> PrepareAsync(
            UploadIntentValidator uploadIntentValidator,
            Guid intentId,
            long userId,
            UploadIntentPurpose purpose,
            IPictureOwnerConfiguration owner,
            CancellationToken cancellationToken)
        {
            var validation = await uploadIntentValidator.ValidateAndLoadAsync(intentId, userId, purpose, cancellationToken);
            if (validation.IsError)
                return validation.Errors;

            var intent = validation.Value;

            return new PreparedPicture(
                intent,
                EntityPictures.Pending(intent.ObjectName),
                owner.Routes.BuildOriginalPicturePath(userId, intent.ObjectName),
                owner.PicturesSizes.ToImageSizes(owner.Routes));
        }
    }
}
