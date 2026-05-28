using System.ComponentModel.DataAnnotations;

namespace WebApi.Options;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    [Required]
    public string OtlpEndpoint { get; init; } = string.Empty;
}
