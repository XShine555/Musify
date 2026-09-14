using Musify.Domain.ValueObjects;

namespace Musify.Api.DataTransferObjects.PlayLists;

public record UpdatePlayListRequest(
    string? NewName,
    string? NewDescription,
    Guid? NewPictureIntentId,
    PlaylistVisibility? NewVisibility = null);
