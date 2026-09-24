using System.ComponentModel.DataAnnotations;

namespace Musify.Application.Configuration
{
    public class StreamGatewayConfiguration : IConfigurationOptions
    {
        public static string SectionName => "StreamGateway";

        [Url]
        [Required]
        public required string PublicBaseUrl { get; set; }

        [Required]
        public string AudioFileName { get; set; } = "audio.m4a";
    }
}
