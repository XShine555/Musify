using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
    public class YtDlpConfiguration
    {
        public const string SectionName = "YtDlp";

        [Required]
        public string ExecutableName { get; set; } = "yt-dlp";

        [Required]
        public string Format { get; set; } = "bestaudio";

        public string AdditionalArguments { get; set; } = string.Empty;

        [Required]
        public TimeSpan DownloadTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}
