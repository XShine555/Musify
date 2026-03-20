using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
    public class OpenTelemetryConfiguration
    {
        public const string SectionName = "OpenTelemetry";

        [Required]
        [Url]
        public required string OtlpEndpoint { get; set; }
    }
}
