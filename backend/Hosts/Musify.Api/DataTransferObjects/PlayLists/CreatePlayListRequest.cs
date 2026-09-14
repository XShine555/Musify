using Musify.Domain.ValueObjects;

namespace Musify.Api.DataTransferObjects.PlayLists;

public record CreatePlayListRequest(
    string Name,
    string? Description,
    Guid? PictureIntentId,
    PlaylistVisibility Visibility = PlaylistVisibility.Private);
