using ErrorOr;
using Mediator;
using Musify.Application.Configuration;
using Musify.Application.Pictures.Responses;
using Musify.Application.Services;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Albums;

public record RequestAlbumPictureUploadCommand(
    long UserId,
    string FileType,
    string ContentType,
    long? ExpectedSizeBytes = null)
    : ICommand<ErrorOr<PictureUploadResponse>>;

public class RequestAlbumPictureUploadCommandHandler(UploadIntentIssuer issuer, AlbumConfiguration albumConfiguration)
    : ICommandHandler<RequestAlbumPictureUploadCommand, ErrorOr<PictureUploadResponse>>
{
    public async ValueTask<ErrorOr<PictureUploadResponse>> Handle(RequestAlbumPictureUploadCommand request, CancellationToken cancellationToken) =>
        await issuer.IssuePictureAsync(
            request.UserId, UploadIntentPurpose.AlbumPicture, albumConfiguration,
            request.FileType, request.ContentType, request.ExpectedSizeBytes, cancellationToken);
}
