using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration;

public class UploadIntentConfiguration
{
    public const string SectionName = "UploadIntent";

    [Range(10, 3600)]
    public int UploadUrlExpiresInSeconds { get; set; } = 120;

    [Range(1, 100)]
    public int MaxActiveUploadIntentsPerUser { get; set; } = 5;

    [Range(1, long.MaxValue)]
    public long MaxActiveUploadBytesPerUser { get; set; } = 200L * 1024 * 1024;

    [Range(1, long.MaxValue)]
    public long MaxUploadBytes { get; set; } = 100L * 1024 * 1024;

    [Range(1, long.MaxValue)]
    public long DefaultExpectedPictureSizeBytes { get; set; } = 10L * 1024 * 1024;

    [Range(1, long.MaxValue)]
    public long DefaultExpectedAudioSizeBytes { get; set; } = 100L * 1024 * 1024;

    [Range(30, 86400)]
    public int ExpirationJobIntervalSeconds { get; set; } = 300;

    [Range(0, 365)]
    public int ExpiredIntentsRetentionDays { get; set; } = 7;

    [Required, MinLength(1)]
    public string TemporalRootPrefix { get; set; } = "temporal";

    [Range(1, 30)]
    public int TemporalUploadsRetentionDays { get; set; } = 3;

    [Range(60, 86400)]
    public int TemporalCleanUpJobIntervalSeconds { get; set; } = 3600;
}
