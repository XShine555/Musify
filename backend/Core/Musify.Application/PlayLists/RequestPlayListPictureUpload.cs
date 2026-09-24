using ErrorOr;
using Mediator;
using Musify.Application.Configuration;
using Musify.Application.Pictures.Responses;
using Musify.Application.Services;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists;

public record RequestPlayListPictureUploadCommand(
    long UserId,
    string FileType,
    string ContentType,
    long? ExpectedSizeBytes = null)
    : ICommand<ErrorOr<PictureUploadResponse>>;

public class RequestPlayListPictureUploadCommandHandler(UploadIntentIssuer issuer, PlayListConfiguration playListConfiguration)
    : ICommandHandler<RequestPlayListPictureUploadCommand, ErrorOr<PictureUploadResponse>>
{
    public async ValueTask<ErrorOr<PictureUploadResponse>> Handle(RequestPlayListPictureUploadCommand request, CancellationToken cancellationToken) =>
        await issuer.IssuePictureAsync(
            request.UserId, UploadIntentPurpose.PlayListPicture, playListConfiguration,
            request.FileType, request.ContentType, request.ExpectedSizeBytes, cancellationToken);
}
