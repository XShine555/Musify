using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Configuration;
using Musify.Application.Tracks.Commands;
using Musify.Application.Tracks.Responses;

namespace Musify.Application.Tracks.Handler
{
    public class RequestTrackUploadUrlsCommandHandler(
        IDatabase database,
        IStorageService storageService,
        ILogger<RequestTrackUploadUrlsCommandHandler> logger,
        ApplicationStorageConfiguration storageConfiguration,
        TrackConfiguration trackConfiguration)
        : ICommandHandler<RequestTrackUploadUrlsCommand, Result<TrackUploadUrlsResponse>>
    {
        static readonly string[] AllowedPictureExtensions = [".webp", ".png", ".jpg", ".jpeg"];
        static readonly string[] AllowedAudioExtensions = [".mp3", ".wav", ".m4a", ".flac", ".ogg", ".aac", ".opus"];
        const int ExpiresInSeconds = 600;

        public async ValueTask<Result<TrackUploadUrlsResponse>> Handle(RequestTrackUploadUrlsCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.UserId, cancellationToken);
            if (!userExists)
            {
                logger.LogWarning("User {UserId} not found", request.UserId);
                return Result.NotFound($"User {request.UserId} not found");
            }

            var pictureExtensionResult = ValidateExtension(request.PictureFileType, AllowedPictureExtensions);
            if (!pictureExtensionResult.IsSuccess)
                return Result.Invalid(pictureExtensionResult.Errors.Select(error => new ValidationError(error)).ToArray());

            var audioExtensionResult = ValidateExtension(request.AudioFileType, AllowedAudioExtensions);
            if (!audioExtensionResult.IsSuccess)
                return Result.Invalid(audioExtensionResult.Errors.Select(error => new ValidationError(error)).ToArray());

            if (string.IsNullOrWhiteSpace(request.PictureContentType))
                return Result.Invalid(new ValidationError("PictureContentType is required"));

            if (string.IsNullOrWhiteSpace(request.AudioContentType))
                return Result.Invalid(new ValidationError("AudioContentType is required"));

            var pictureName = Guid.NewGuid() + pictureExtensionResult.Value;
            var audioName = Guid.NewGuid() + audioExtensionResult.Value;

            var pictureKey = trackConfiguration.Routes.BuildOriginalPicturePath(request.UserId, pictureName);
            var audioKey = trackConfiguration.Routes.BuildOriginalAudioPath(request.UserId, audioName);

            try
            {
                var pictureUploadUrl = await storageService.GetUploadUrlAsync(
                    storageConfiguration.Bucket,
                    pictureKey,
                    request.PictureContentType,
                    TimeSpan.FromSeconds(ExpiresInSeconds),
                    cancellationToken);

                var audioUploadUrl = await storageService.GetUploadUrlAsync(
                    storageConfiguration.Bucket,
                    audioKey,
                    request.AudioContentType,
                    TimeSpan.FromSeconds(ExpiresInSeconds),
                    cancellationToken);

                return Result.Success(new TrackUploadUrlsResponse(
                    storageConfiguration.Bucket,
                    pictureKey,
                    pictureName,
                    request.PictureContentType,
                    pictureUploadUrl,
                    audioKey,
                    audioName,
                    request.AudioContentType,
                    audioUploadUrl,
                    ExpiresInSeconds));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to generate track upload URLs for user {UserId}", request.UserId);
                return Result.Error("Failed to generate upload URLs");
            }
        }

        static Result<string> ValidateExtension(string fileType, string[] allowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(fileType))
                return Result.Error("FileType is required");

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
