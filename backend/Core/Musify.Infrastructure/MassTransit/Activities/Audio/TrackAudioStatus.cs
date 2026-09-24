using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Infrastructure.MassTransit.Activities.Audio
{
    /// <summary>Shared bookkeeping of the audio processing activities.</summary>
    internal static class TrackAudioStatus
    {
        public static async Task<Track> LoadAsync(IDatabase database, Guid trackId, CancellationToken cancellationToken) =>
            await database.Tracks.SingleOrDefaultAsync(track => track.Id == trackId, cancellationToken)
                ?? throw new InvalidOperationException($"Track with id {trackId} not found");

        public static async Task MarkFailedAsync(IDatabase database, Guid trackId, CancellationToken cancellationToken)
        {
            var track = await database.Tracks.SingleOrDefaultAsync(t => t.Id == trackId, cancellationToken);
            if (track == null)
                return;

            track.Audio.TranscodeStatus = ProcessingStatus.Failed;
            await database.SaveChangesAsync(cancellationToken);
        }
    }
}
