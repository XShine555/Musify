using Ardalis.Result;

namespace Musify.Application.UploadIntents
{
    internal static class UploadIntentHelpers
    {
        internal static Result<string> ValidateExtension(string fileType, string[] allowedExtensions)
        {
            var extension = fileType.Trim();
            if (!extension.StartsWith('.'))
                extension = "." + extension;

            extension = extension.ToLowerInvariant();

            if (extension.Contains('/') || extension.Contains('\\'))
                return Result.Error("Invalid FileType");

            if (!allowedExtensions.Contains(extension))
                return Result.Error($"Unsupported FileType '{extension}'");

            return Result.Success(extension);
        }
    }
}
