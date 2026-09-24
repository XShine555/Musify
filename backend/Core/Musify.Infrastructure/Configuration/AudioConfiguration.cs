using System.ComponentModel.DataAnnotations;
using Musify.Application.Configuration;

namespace Musify.Infrastructure.Configuration
{
    public class AudioConfiguration : IConfigurationOptions
    {
        public static string SectionName => "AudioTranscoder";

        [Required]
        public TimeSpan TranscodingTimeout { get; set; } = TimeSpan.FromMinutes(5);

        [Required]
        public FfmpegConfiguration Ffmpeg { get; set; } = new FfmpegConfiguration();
    }

    public class FfmpegConfiguration
    {
        [Required]
        public string ExecutableName { get; set; } = "ffmpeg";

        [Required]
        public string OutputFileName { get; set; } = "audio.m4a";

        [Required]
        public string AudioCodec { get; set; } = "aac";

        [Required]
        public string AudioBitrate { get; set; } = "128k";

        [Range(1, 8)]
        public int AudioChannels { get; set; } = 2;

        [Range(8000, 192000)]
        public int AudioSampleRate { get; set; } = 48000;

        [Required]
        public string AudioProfile { get; set; } = "aac_low";

        public string AdditionalArguments { get; set; } = string.Empty;
    }
}
