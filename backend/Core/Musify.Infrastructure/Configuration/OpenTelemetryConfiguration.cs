using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration;

public class OpenTelemetryConfiguration
{
    public const string SectionName = "OpenTelemetry";

    [Url]
    [Required]
    public required string OtlpEndpoint { get; set; }
}
