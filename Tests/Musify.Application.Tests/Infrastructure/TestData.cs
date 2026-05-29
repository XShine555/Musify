using Musify.Application.Configuration;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tests.Infrastructure;

public static class TestData
{
    public const string Bucket = "musify-test";

    public static ApplicationStorageConfiguration StorageConfig() => new() { Bucket = Bucket };

    public static UploadIntentConfiguration UploadIntentConfig() => new();

    public static TrackConfiguration TrackConfig() => new();

    public static PlayListConfiguration PlayListConfig() => new();

    public static User User(Guid? id = null, string name = "Alice") => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
    };

    public static UploadIntent UploadIntent(
        Guid userId,
        UploadIntentPurpose purpose = UploadIntentPurpose.TrackPicture,
        UploadIntentStatus status = UploadIntentStatus.Issued,
        DateTime? expiresAt = null,
        long? expectedSizeBytes = null,
        string objectName = "object.webp",
        string? key = null) => new()
    {
        UserId = userId,
        Bucket = Bucket,
        Key = key ?? $"temp/{userId}/Tracks/{objectName}",
        ObjectName = objectName,
        ContentType = "image/webp",
        ExpectedSizeBytes = expectedSizeBytes,
        Purpose = purpose,
        Status = status,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(5),
    };

    public static Track Track(
        string title = "Song",
        ProcessingStatus audioStatus = ProcessingStatus.Completed,
        LifeCycleStatus lifeCycle = LifeCycleStatus.Active) => new()
    {
        Title = title,
        NormalizedTitle = title.ToUpperInvariant(),
        OriginalPictureName = "pic.webp",
        OriginalAudioName = "audio.mp3",
        AudioTranscodeProcessingStatus = audioStatus,
        PicturesProcessingStatus = ProcessingStatus.Completed,
        LifeCycleStatus = lifeCycle,
    };

    public static PlayList PlayList(
        Guid userId,
        string name = "My PlayList",
        LifeCycleStatus lifeCycle = LifeCycleStatus.Active) => new()
    {
        UserId = userId,
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        Description = "desc",
        OriginalPictureName = "orig.webp",
        SmallPictureName = "small.webp",
        MediumPictureName = "medium.webp",
        LargePictureName = "large.webp",
        LifeCycleStatus = lifeCycle,
    };
}
