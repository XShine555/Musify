using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Mixes.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.Mixes
{
    /// <summary>Resolves MixItem rows to their MixItemApplicationResponse shape via a single batched
    /// track lookup, instead of an EF Include chain deep enough (Mix -&gt; Items -&gt; Track -&gt; Owner) to
    /// force a correlated-subquery translation some providers (SQLite, notably) can't produce.</summary>
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
                .ToDictionaryAsync(track => track.Id, cancellationToken);

            return orderedItems
                .Where(item => tracksById.ContainsKey(item.TrackId))
                .Select(item =>
                {
                    var track = tracksById[item.TrackId];
                    return new MixItemApplicationResponse(item.TrackId, track.Title, track.Owner.Name, track.DurationSeconds);
                })
                .ToList();
        }
    }
}
