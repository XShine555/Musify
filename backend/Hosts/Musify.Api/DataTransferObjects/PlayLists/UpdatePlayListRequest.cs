using Musify.Domain.ValueObjects;

namespace Musify.Api.DataTransferObjects.PlayLists
{
    public record UpdatePlayListRequest(
        string Name,
        string? Description,
        PlayListVisibility? Visibility);
}
