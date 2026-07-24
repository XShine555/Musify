using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Services;

public sealed record YouTubeTrackProvision(ExternalTrack Track, YouTubeSongResult? Song);

public sealed class YouTubeTrackProvisioner(
    IDatabase database,
    IYouTubeMusicService youTubeMusicService,
    TrackConfiguration trackConfiguration,
    ILogger<YouTubeTrackProvisioner> logger)
{
    public Task<ExternalTrack?> FindAsync(string videoId, CancellationToken cancellationToken) =>
        database.ExternalTracks
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

        var title = Truncate(song.Title, 200);
        var track = new ExternalTrack
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            Source = TrackSource.YouTube,
            ExternalId = videoId,
            DurationSeconds = song.DurationSeconds,
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

        await database.ExternalTracks.AddAsync(track, cancellationToken);

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
            database.ExternalTracks.Remove(track);
            var refetched = await FindAsync(videoId, cancellationToken);
            if (refetched is null)
            {
                logger.LogWarning("YouTube track {VideoId} creation raced but could not be refetched", videoId);
                return Error.Conflict(description: "The track was just created by another request. Retry the operation.");
            }
            return new YouTubeTrackProvision(refetched, Song: null);
        }
    }

    public async Task MaterializeAlbumAsync(string albumExternalId, YouTubeAlbumDetail detail, CancellationToken cancellationToken)
    {
        var validTracks = detail.Tracks
            .Where(track => !string.IsNullOrEmpty(track.VideoId))
            .ToList();
        if (validTracks.Count == 0)
            return;

        var album = await GetOrCreateExternalAlbumAsync(albumExternalId, detail.Album.Title, detail.Album.ThumbnailUrl, cancellationToken);

        var videoIds = validTracks.Select(track => track.VideoId).ToList();

        var tracksByVideoId = await database.ExternalTracks
            .Where(track => track.Source == TrackSource.YouTube && videoIds.Contains(track.ExternalId))
            .ToDictionaryAsync(track => track.ExternalId, cancellationToken);

        var linkedTrackIds = (await database.AlbumHasTracks
            .Where(albumTrack => albumTrack.AlbumId == album.Id)
            .Select(albumTrack => albumTrack.TrackId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        if (tracksByVideoId.Count == validTracks.Count && linkedTrackIds.Count >= validTracks.Count)
            return;

        var artistRefs = detail.Album.Artists
            .GroupBy(artistRef => artistRef.Id ?? artistRef.Name.ToUpperInvariant())
            .Select(group => group.First())
            .ToList();

        var albumArtists = new List<Artist>();
        foreach (var artistRef in artistRefs)
            albumArtists.Add(await GetOrCreateArtistAsync(artistRef, cancellationToken));

        foreach (var albumTrack in validTracks)
        {
            if (!tracksByVideoId.TryGetValue(albumTrack.VideoId, out var track))
            {
                track = BuildStubTrack(albumTrack);
                await database.ExternalTracks.AddAsync(track, cancellationToken);

                for (var position = 0; position < albumArtists.Count; position++)
                {
                    await database.TrackArtists.AddAsync(new TrackArtist
                    {
                        TrackId = track.Id,
                        ArtistId = albumArtists[position].Id,
                        Position = position
                    }, cancellationToken);
                }

                tracksByVideoId[albumTrack.VideoId] = track;
            }

            if (linkedTrackIds.Contains(track.Id))
                continue;

            await database.AlbumHasTracks.AddAsync(new AlbumHasTrack
            {
                AlbumId = album.Id,
                TrackId = track.Id,
                TrackNumber = albumTrack.TrackNumber
            }, cancellationToken);
            linkedTrackIds.Add(track.Id);
        }

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Materialized YouTube album {AlbumId} ({TrackCount} tracks)", albumExternalId, validTracks.Count);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Race while materializing YouTube album {AlbumId}", albumExternalId);
        }
    }

    private ExternalTrack BuildStubTrack(YouTubeAlbumTrack albumTrack)
    {
        var title = Truncate(albumTrack.Title, 200);
        return new ExternalTrack
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            Source = TrackSource.YouTube,
            ExternalId = albumTrack.VideoId,
            DurationSeconds = albumTrack.DurationSeconds,
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
    }

    private async Task<Artist> GetOrCreateArtistAsync(YouTubeArtistRef reference, CancellationToken cancellationToken)
    {
        var name = Truncate(reference.Name, 200);
        var normalized = name.ToUpperInvariant();
        var externalId = reference.Id ?? $"name:{normalized}";

        var existing = await database.Artists.SingleOrDefaultAsync(a => a.ExternalId == externalId, cancellationToken);
        if (existing is not null)
            return existing;

        var artist = new Artist
        {
            Name = name,
            NormalizedName = normalized,
            ExternalId = externalId
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
            return await database.Artists.SingleAsync(a => a.ExternalId == externalId, cancellationToken);
        }
    }

    private async Task<ExternalAlbum> GetOrCreateExternalAlbumAsync(string externalId, string name, string? thumbnailUrl, CancellationToken cancellationToken)
    {
        var existing = await database.ExternalAlbums
            .SingleOrDefaultAsync(album => album.Source == TrackSource.YouTube && album.ExternalId == externalId, cancellationToken);
        if (existing is not null)
            return existing;

        var title = Truncate(name, 200);
        var album = new ExternalAlbum
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            Source = TrackSource.YouTube,
            ExternalId = externalId,
            ThumbnailUrl = string.IsNullOrEmpty(thumbnailUrl) ? null : Truncate(thumbnailUrl, 512)
        };

        await database.ExternalAlbums.AddAsync(album, cancellationToken);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return album;
        }
        catch (DbUpdateException)
        {
            database.ExternalAlbums.Remove(album);
            return await database.ExternalAlbums.SingleAsync(a => a.Source == TrackSource.YouTube && a.ExternalId == externalId, cancellationToken);
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
