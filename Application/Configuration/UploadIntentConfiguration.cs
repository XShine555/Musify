using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class UploadIntentConfiguration
    {
        public const string SectionName = "UploadIntent";

        // Pre-signed URL TTL in seconds
        [Range(10, 3600)]
        public int UploadUrlExpiresInSeconds { get; set; } = 120;

        // Max concurrent Issued intents per user
        [Range(1, 100)]
        public int MaxActiveUploadIntentsPerUser { get; set; } = 5;

        // Max total bytes across all Issued intents for a user (default 200MB)
        [Range(1, long.MaxValue)]
        public long MaxActiveUploadBytesPerUser { get; set; } = 200L * 1024 * 1024;

        // Max size validated via HEAD before consuming an intent (default 100MB)
        [Range(1, long.MaxValue)]
        public long MaxUploadBytes { get; set; } = 100L * 1024 * 1024;

        // Conservative fallback size used when ExpectedSizeBytes is not provided
        [Range(1, long.MaxValue)]
        public long DefaultExpectedPictureSizeBytes { get; set; } = 10L * 1024 * 1024;   // 10MB

        [Range(1, long.MaxValue)]
        public long DefaultExpectedAudioSizeBytes { get; set; } = 100L * 1024 * 1024;    // 100MB

        // How often the expiration job runs
        [Range(30, 86400)]
        public int ExpirationJobIntervalSeconds { get; set; } = 300;

        // How many days to retain Expired intents before deleting the DB row
        [Range(0, 365)]
        public int ExpiredIntentsRetentionDays { get; set; } = 7;

        // Root S3 prefix for temporary uploads (Phase 2)
        [Required, MinLength(1)]
        public string TempRootPrefix { get; set; } = "temp";

        // How many days to retain orphaned temp objects in S3 before the cleanup job deletes them
        [Range(1, 30)]
        public int TempUploadsRetentionDays { get; set; } = 3;

        // How often the temp-upload cleanup job runs
        [Range(60, 86400)]
        public int TempCleanupJobIntervalSeconds { get; set; } = 3600;
    }
}
