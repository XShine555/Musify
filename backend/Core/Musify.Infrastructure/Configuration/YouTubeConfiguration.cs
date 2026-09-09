using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
    public class YouTubeConfiguration
    {
        public const string SectionName = "YouTube";

        /// <summary>Turns the whole YouTube Music source on or off: search, albums, streaming
        /// and adding new YouTube tracks all fail with a clear error while this is <c>false</c>,
        /// and mix generation silently skips YouTube candidates instead of failing.</summary>
        public bool Enabled { get; set; } = true;

        [Required]
        public string GeographicalLocation { get; set; } = "US";

        [Range(60, 21600)]
        public int StreamUrlCacheSeconds { get; set; } = 1800;

        [Range(60, 3600)]
        public int SearchContinuationCacheSeconds { get; set; } = 600;

        [Range(0, 86400)]
        public int SearchResultsCacheSeconds { get; set; } = 900;

        [Range(0, 604800)]
        public int SongInfoCacheSeconds { get; set; } = 86400;

        [Range(32, 720)]
        public int ThumbnailSize { get; set; } = 240;

        [Range(120, 1080)]
        public int ArtworkSize { get; set; } = 544;
    }
}
