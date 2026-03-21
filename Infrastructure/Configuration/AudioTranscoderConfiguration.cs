using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
#pragma warning disable CS8618
    public class AudioTranscoderConfiguration
    {
        public const string SectionName = "AudioTranscoder";

        [Required]
        public AudioTranscoderRoutes Routes { get; set; } = new AudioTranscoderRoutes();

        [Required]
        public TimeSpan TranscodingTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    public class AudioTranscoderRoutes
    {
        [Required]
        public string WorkingDirectory { get; set; }
    }
}