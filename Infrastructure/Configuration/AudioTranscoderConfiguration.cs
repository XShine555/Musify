using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
    public class AudioTranscoderConfiguration
    {
        public const string SectionName = "AudioTranscoder";

        [Required]
        public TimeSpan TranscodingTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}