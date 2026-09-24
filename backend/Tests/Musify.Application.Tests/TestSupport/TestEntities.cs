using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tests.TestSupport;

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

    public static Track Track(
        User owner,
        string title = "Test Track",
        double durationSeconds = 180,
        TrackPictures? pictures = null,
        TrackAudio? audio = null,
        LifeCycleStatus lifeCycleStatus = LifeCycleStatus.Active,
        IEnumerable<Genre>? tags = null,
        bool isExplicit = false)
    {
        var track = new Track
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            DurationSeconds = durationSeconds,
            LifeCycleStatus = lifeCycleStatus,
            IsExplicit = isExplicit,
            OwnerUserId = owner.Id,
            Owner = owner,
            Pictures = pictures ?? ProcessedPictures(),
            Audio = audio ?? ProcessedAudio()
        };

        track.Tags = (tags ?? [Genre.Pop])
            .Select(tag => new TrackTag { TrackId = track.Id, Tag = tag })
            .ToList();

        return track;
    }

    public static EntityPictures PlayListPictures() => new()
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
        EntityPictures? pictures = null,
        LifeCycleStatus lifeCycleStatus = LifeCycleStatus.Active,
        PlayListVisibility visibility = PlayListVisibility.Private) =>
        new()
        {
            OwnerUserId = userId,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = description,
            Pictures = pictures ?? PlayListPictures(),
            LifeCycleStatus = lifeCycleStatus,
            Visibility = visibility
        };

    public static EntityPictures AlbumPictures() => new()
    {
        OriginalName = "original.webp",
        SmallName = "small.webp",
        MediumName = "medium.webp",
        LargeName = "large.webp"
    };

    public static Album Album(
        long ownerUserId,
        string title = "Test Album",
        string? description = null,
        int? releaseYear = 2024,
        EntityPictures? pictures = null) =>
        new()
        {
            Title = title,
            NormalizedTitle = title.ToUpperInvariant(),
            Description = description,
            ReleaseYear = releaseYear,
            OwnerUserId = ownerUserId,
            Pictures = pictures ?? AlbumPictures()
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
        Guid trackId,
        int position = 0) =>
        new()
        {
            MixId = mixId,
            Position = position,
            TrackId = trackId
        };

    public static ListeningHistory ListeningHistory(
        long userId,
        Guid trackId,
        DateTime? listenedAt = null,
        double? playedSeconds = null,
        bool isCounted = true) =>
        new()
        {
            UserId = userId,
            TrackId = trackId,
            ListenedAt = listenedAt ?? DateTime.UtcNow,
            PlayedSeconds = playedSeconds,
            IsCounted = isCounted
        };
}
