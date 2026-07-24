using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Mixes.Responses
{
    public record MixItemApplicationResponse(
        MixItemSource Source,
        Guid? TrackId,
        string? VideoId,
        string Title,
        string? Artist,
        string? ThumbnailUrl,
        double DurationSeconds,
        bool IsExplicit)
    {
        public static MixItemApplicationResponse FromEntity(MixItem item) =>
            new(
                item.Source,
                item.TrackId,
                item.VideoId,
                item.Title ?? string.Empty,
                item.Artist,
                item.ThumbnailUrl,
                item.DurationSeconds,
                item.IsExplicit);
    }
}
