using Musify.Domain.ValueObjects;

namespace Musify.Api.DataTransferObjects.PlayLists
{
    public record UpdatePlayListRequest(
        string? Name,
        string? Description,
        Guid? PictureIntentId,
        PlayListVisibility? Visibility = null);
}
