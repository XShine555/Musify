using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Mixes.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.Mixes;


internal static class MixItemMapper
{
    public static async Task<List<MixItemApplicationResponse>> ToResponsesAsync(
        IDatabase database,
        IReadOnlyList<MixItem> orderedItems,
        CancellationToken cancellationToken)
    {
        var trackIds = orderedItems.Select(item => item.TrackId).Distinct().ToList();

        var tracksById = await database.Tracks
            .AsNoTracking()
            .Include(track => track.Owner)
            .Where(track => trackIds.Contains(track.Id))
            .Select(track => new { Track = track, ListensCount = track.ListeningHistories.Count(l => l.IsCounted) })
            .ToDictionaryAsync(x => x.Track.Id, cancellationToken);

        return orderedItems
            .Where(item => tracksById.ContainsKey(item.TrackId))
            .Select(item =>
            {
                var entry = tracksById[item.TrackId];
                return new MixItemApplicationResponse(
                    item.TrackId,
                    entry.Track.Title,
                    entry.Track.Owner.Name,
                    entry.Track.DurationSeconds,
                    entry.ListensCount);
            })
            .ToList();
    }
}
