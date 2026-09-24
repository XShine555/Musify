using System.ComponentModel.DataAnnotations;
using Musify.Application.Configuration;

namespace Musify.Infrastructure.Configuration;

public class OpenTelemetryConfiguration : IConfigurationOptions
{
    public static string SectionName => "OpenTelemetry";

    [Url]
    [Required]
    public required string OtlpEndpoint { get; set; }
}
