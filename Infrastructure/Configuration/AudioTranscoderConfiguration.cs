using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
    public class AudioTranscoderConfiguration
    {
        public const string SectionName = "AudioTranscoder";

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
        public string ManifestFileName { get; set; } = "manifest.mpd";

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

        [Range(1, 60)]
        public int SegmentDurationSeconds { get; set; } = 4;

        [Required]
        public string InitSegmentName { get; set; } = "init-stream$RepresentationID$.m4s";

        [Required]
        public string MediaSegmentName { get; set; } = "chunk-stream$RepresentationID$-$Number$.m4s";

        public string AdditionalArguments { get; set; } = string.Empty;
    }
}