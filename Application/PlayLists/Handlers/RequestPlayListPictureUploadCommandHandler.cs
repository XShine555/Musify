using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Configuration;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Responses;

namespace Musify.Application.PlayLists.Handlers
{
    public class RequestPlayListPictureUploadCommandHandler(
        IDatabase database,
        IStorageService storageService,
        ILogger<RequestPlayListPictureUploadCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        PlayListConfiguration playListConfiguration)
        : ICommandHandler<RequestPlayListPictureUploadCommand, Result<PlayListPictureUploadResponse>>
    {
        static readonly string[] AllowedExtensions = [".webp", ".png", ".jpg", ".jpeg"];
        const int ExpiresInSeconds = 600;

        public async ValueTask<Result<PlayListPictureUploadResponse>> Handle(RequestPlayListPictureUploadCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.NotFound($"User {request.UserId} not found");
            }

            var extensionResult = ValidateExtension(request.FileType);
            if (!extensionResult.IsSuccess)
                return Result.Invalid(extensionResult.Errors.Select(error => new ValidationError(error)).ToArray());

            if (string.IsNullOrWhiteSpace(request.ContentType))
                return Result.Invalid(new ValidationError("ContentType is required"));

            var pictureName = Guid.NewGuid() + extensionResult.Value;
            var key = playListConfiguration.Routes.BuildOriginalPicturePath(request.UserId, pictureName);

            try
            {
                var uploadUrl = await storageService.GetUploadUrlAsync(
                    storageConfiguration.Bucket,
                    key,
                    request.ContentType,
                    TimeSpan.FromSeconds(ExpiresInSeconds),
                    cancellationToken);

                return Result.Success(new PlayListPictureUploadResponse(
                    storageConfiguration.Bucket,
                    key,
                    pictureName,
                    request.ContentType,
                    ExpiresInSeconds,
                    uploadUrl));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to generate playlist picture upload URL for user {UserId}", request.UserId);
                return Result.Error("Failed to generate upload URL");
            }
        }

        static Result<string> ValidateExtension(string fileType)
        {
            if (string.IsNullOrWhiteSpace(fileType))
                return Result.Error("FileType is required");

            var extension = fileType.Trim();
            if (!extension.StartsWith('.'))
                extension = "." + extension;

            extension = extension.ToLowerInvariant();

            if (extension.Contains('/') || extension.Contains('\\'))
                return Result.Error("Invalid FileType");

            if (!AllowedExtensions.Contains(extension))
                return Result.Error($"Unsupported FileType '{extension}'");

            return Result.Success(extension);
        }
    }
}
