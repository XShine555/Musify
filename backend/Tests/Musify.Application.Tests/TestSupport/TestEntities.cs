using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tests.TestSupport;

/// <summary>
/// Builds fully-populated domain entities for tests, with sensible defaults for every
/// <c>required</c> member. Every factory always returns a valid instance — never null — so
/// callers never need a null check on what this class hands back.
/// </summary>
public static class TestEntities
{
    public static User User(
        long id = 1,
        string name = "test-user",
        string? firstName = "Test",
        string? secondName = "User",
        string? profilePictureUrl = null) =>
        new()
        {
            Id = id,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            FirstName = firstName,
            SecondName = secondName,
            ProfilePictureUrl = profilePictureUrl
        };

    public static TrackPictures ProcessedPictures() => new()
    {
        OriginalName = "original.webp",
        SmallName = "small.webp",
        MediumName = "medium.webp",
        LargeName = "large.webp",
        ProcessingStatus = ProcessingStatus.Completed
    };

    public static TrackPictures PendingPictures() => new()
    {
        OriginalName = "original.webp",
        ProcessingStatus = ProcessingStatus.Pending
    };

    public static TrackAudio ProcessedAudio(string folderName = "audio-folder") => new()
    {
        OriginalName = "original.mp3",
        FolderName = folderName,
        TranscodeStatus = ProcessingStatus.Completed
    };

    public static TrackAudio PendingAudio() => new()
    {
        OriginalName = "original.mp3",
        TranscodeStatus = ProcessingStatus.Pending
    };

    public static LocalTrack LocalTrack(
        User owner,
        string title = "Test Track",
        double durationSeconds = 180,
        TrackPictures? pictures = null,
        TrackAudio? audio = null,
        LifeCycleStatus lifeCycleStatus = LifeCycleStatus.Active) =>
        new()
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            DurationSeconds = durationSeconds,
            LifeCycleStatus = lifeCycleStatus,
            OwnerUserId = owner.Id,
            Owner = owner,
            Pictures = pictures ?? ProcessedPictures(),
            Audio = audio ?? ProcessedAudio()
        };

    public static ExternalTrack ExternalTrack(
        string externalId = "video-id",
        string title = "Test External Track",
        double durationSeconds = 180,
        TrackSource source = TrackSource.YouTube,
        TrackPictures? pictures = null,
        TrackAudio? audio = null,
        LifeCycleStatus lifeCycleStatus = LifeCycleStatus.Active) =>
        new()
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            DurationSeconds = durationSeconds,
            LifeCycleStatus = lifeCycleStatus,
            Source = source,
            ExternalId = externalId,
            Pictures = pictures ?? ProcessedPictures(),
            Audio = audio ?? ProcessedAudio()
        };

    public static Artist Artist(
        string name = "Test Artist",
        string externalId = "artist-id") =>
        new()
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ExternalId = externalId
        };

    public static PlayListPictures PlayListPictures() => new()
    {
        OriginalName = "original.webp",
        SmallName = "small.webp",
        MediumName = "medium.webp",
        LargeName = "large.webp"
    };

    public static PlayList PlayList(
        long userId,
        string name = "Test Playlist",
        string? description = null,
        PlayListPictures? pictures = null,
        LifeCycleStatus lifeCycleStatus = LifeCycleStatus.Active) =>
        new()
        {
            UserId = userId,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = description,
            Pictures = pictures ?? PlayListPictures(),
            LifeCycleStatus = lifeCycleStatus
        };

    public static AlbumPictures AlbumPictures() => new()
    {
        OriginalName = "original.webp",
        SmallName = "small.webp",
        MediumName = "medium.webp",
        LargeName = "large.webp"
    };

    public static UserAlbum UserAlbum(
        long ownerUserId,
        string title = "Test Album",
        string? description = null,
        int? releaseYear = 2024,
        AlbumPictures? pictures = null) =>
        new()
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            Description = description,
            ReleaseYear = releaseYear,
            OwnerUserId = ownerUserId,
            Pictures = pictures ?? AlbumPictures()
        };

    public static ExternalAlbum ExternalAlbum(
        string externalId = "album-id",
        string title = "Test External Album",
        TrackSource source = TrackSource.YouTube,
        string? thumbnailUrl = null) =>
        new()
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            Source = source,
            ExternalId = externalId,
            ThumbnailUrl = thumbnailUrl
        };

    public static UploadIntent UploadIntent(
        long userId,
        UploadIntentPurpose purpose = UploadIntentPurpose.TrackPicture,
        string bucket = "test-bucket",
        string key = "temp/object.webp",
        string objectName = "object.webp",
        string contentType = "image/webp",
        long? expectedSizeBytes = 1024,
        UploadIntentStatus status = UploadIntentStatus.Issued,
        DateTime? expiresAt = null) =>
        new()
        {
            UserId = userId,
            Bucket = bucket,
            Key = key,
            ObjectName = objectName,
            ContentType = contentType,
            ExpectedSizeBytes = expectedSizeBytes,
            Purpose = purpose,
            Status = status,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(5)
        };

    public static Mix Mix(
        long userId,
        string title = "Test Mix",
        string? subtitle = null,
        int position = 0) =>
        new()
        {
            UserId = userId,
            Title = title,
            Subtitle = subtitle,
            Position = position
        };

    public static MixItem MixItem(
        Guid mixId,
        int position = 0,
        MixItemSource source = MixItemSource.Musify,
        Guid? trackId = null,
        string? videoId = null,
        string title = "Test Mix Item",
        string? artist = "Test Artist",
        double durationSeconds = 180,
        bool isExplicit = false) =>
        new()
        {
            MixId = mixId,
            Position = position,
            Source = source,
            TrackId = trackId,
            VideoId = videoId,
            Title = title,
            Artist = artist,
            DurationSeconds = durationSeconds,
            IsExplicit = isExplicit
        };

    public static ListeningHistory ListeningHistory(
        long userId,
        Guid trackId,
        DateTime? listenedAt = null) =>
        new()
        {
            UserId = userId,
            TrackId = trackId,
            ListenedAt = listenedAt ?? DateTime.UtcNow
        };
}
