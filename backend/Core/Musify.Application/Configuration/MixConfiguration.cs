using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class MixConfiguration
    {
        public const string SectionName = "Mix";

        [Range(1, 20)]
        public int ArtistMixCount { get; set; } = 3;

        [Range(4, 100)]
        public int ItemsPerMix { get; set; } = 12;

        [Range(1, 10)]
        public int SeedArtistCount { get; set; } = 3;

        [Range(1, 100)]
        public int YouTubeResultsPerSeed { get; set; } = 12;
    }
}
