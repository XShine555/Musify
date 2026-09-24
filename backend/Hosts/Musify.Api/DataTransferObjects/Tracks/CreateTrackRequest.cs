using Musify.Domain.ValueObjects;

namespace Musify.Api.DataTransferObjects.Tracks
{
    public record CreateTrackRequest(
        string Title,
        Guid PictureIntentId,
        Guid AudioIntentId,
        IReadOnlyCollection<Genre> Tags,
        bool IsExplicit = false);
}
