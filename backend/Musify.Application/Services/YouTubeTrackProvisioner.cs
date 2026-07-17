using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Services;

public sealed record YouTubeTrackProvision(Track Track, YouTubeSongResult? Song);

public sealed class YouTubeTrackProvisioner(
    IDatabase database,
    IYouTubeMusicService youTubeMusicService,
    TrackConfiguration trackConfiguration,
    ILogger<YouTubeTrackProvisioner> logger)
{
    public Task<Track?> FindAsync(string videoId, CancellationToken cancellationToken) =>
        database.Tracks
            .Include(track => track.TrackArtists)
            .ThenInclude(trackArtist => trackArtist.Artist)
            .SingleOrDefaultAsync(t => t.Source == TrackSource.YouTube && t.ExternalId == videoId, cancellationToken);

    public async Task<ErrorOr<YouTubeTrackProvision>> GetOrCreateAsync(string videoId, CancellationToken cancellationToken)
    {
        var existing = await FindAsync(videoId, cancellationToken);
        if (existing is not null)
            return new YouTubeTrackProvision(existing, Song: null);

        var songResult = await youTubeMusicService.GetSongAsync(videoId, cancellationToken);
        if (songResult.IsError)
            return songResult.Errors;

        var song = songResult.Value;

        var artistRefs = song.Artists
            .GroupBy(artistRef => artistRef.Id ?? artistRef.Name.ToUpperInvariant())
            .Select(group => group.First())
            .ToList();

        var artists = new List<Artist>();
        foreach (var artistRef in artistRefs)
            artists.Add(await GetOrCreateArtistAsync(artistRef, cancellationToken));

        var title = Truncate(song.Title, 50);
        var track = new Track
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            Source = TrackSource.YouTube,
            ExternalId = videoId,
            Duration = song.DurationSeconds,
            Pictures = new TrackPictures
            {
                SmallName = trackConfiguration.Routes.PresetSmallPicture,
                MediumName = trackConfiguration.Routes.PresetMediumPicture,
                LargeName = trackConfiguration.Routes.PresetLargePicture,
                ProcessingStatus = ProcessingStatus.Pending
            },
            Audio = new TrackAudio
            {
                DownloadRequested = false,
                TranscodeStatus = ProcessingStatus.Pending
            }
        };

        await database.Tracks.AddAsync(track, cancellationToken);

        for (var position = 0; position < artists.Count; position++)
        {
            await database.TrackArtists.AddAsync(new TrackArtist
            {
                TrackId = track.Id,
                ArtistId = artists[position].Id,
                Position = position
            }, cancellationToken);
        }

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created YouTube track {VideoId} ({TrackId})", videoId, track.Id);
            return new YouTubeTrackProvision(track, Song: song);
        }
        catch (DbUpdateException)
        {
            database.Tracks.Remove(track);
            var refetched = await FindAsync(videoId, cancellationToken);
            if (refetched is null)
            {
                logger.LogWarning("YouTube track {VideoId} creation raced but could not be refetched", videoId);
                return Error.Conflict(description: "The track was just created by another request. Retry the operation.");
            }
            return new YouTubeTrackProvision(refetched, Song: null);
        }
    }

    private async Task<Artist> GetOrCreateArtistAsync(YouTubeArtistRef reference, CancellationToken cancellationToken)
    {
        var name = Truncate(reference.Name, 200);
        var normalized = name.ToUpperInvariant();
        var byExternalId = !string.IsNullOrEmpty(reference.Id);

        var existing = byExternalId
            ? await database.Artists.SingleOrDefaultAsync(a => a.ExternalId == reference.Id, cancellationToken)
            : await database.Artists.SingleOrDefaultAsync(a => a.ExternalId == null && a.UserId == null && a.NormalizedName == normalized, cancellationToken);
        if (existing is not null)
            return existing;

        if (byExternalId)
        {
            var adoptable = await database.Artists
                .SingleOrDefaultAsync(a => a.ExternalId == null && a.UserId == null && a.NormalizedName == normalized, cancellationToken);
            if (adoptable is not null)
            {
                var previousName = adoptable.Name;
                adoptable.ExternalId = reference.Id;
                adoptable.Name = name;

                try
                {
                    await database.SaveChangesAsync(cancellationToken);
                    return adoptable;
                }
                catch (DbUpdateException)
                {
                    adoptable.ExternalId = null;
                    adoptable.Name = previousName;
                    return await database.Artists.SingleAsync(a => a.ExternalId == reference.Id, cancellationToken);
                }
            }
        }

        var artist = new Artist
        {
            Name = name,
            NormalizedName = normalized,
            ExternalId = byExternalId ? reference.Id : null
        };

        await database.Artists.AddAsync(artist, cancellationToken);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return artist;
        }
        catch (DbUpdateException)
        {
            database.Artists.Remove(artist);
            return byExternalId
                ? await database.Artists.SingleAsync(a => a.ExternalId == reference.Id, cancellationToken)
                : await database.Artists.SingleAsync(a => a.ExternalId == null && a.UserId == null && a.NormalizedName == normalized, cancellationToken);
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
