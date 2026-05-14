using Ardalis.Result;
using Musify.Domain.Entities;

namespace Musify.Application.UploadIntents
{
    internal sealed record IntentValidationResult(Result? Error, UploadIntent? Intent)
    {
        public bool IsSuccess => Error is null;

        public static IntentValidationResult Ok(UploadIntent intent) => new(null, intent);
        public static IntentValidationResult Fail(Result error) => new(error, null);
    }
}
