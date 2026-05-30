using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class StreamGatewayConfiguration
    {
        public const string SectionName = "StreamGateway";

        [Url]
        [Required]
        public required string PublicBaseUrl { get; set; }

        [Required]
        public string ManifestFileName { get; set; } = "manifest.mpd";
    }
}
