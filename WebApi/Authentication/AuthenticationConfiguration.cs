using System.ComponentModel.DataAnnotations;

namespace WebApi.Authentication;

public sealed class AuthenticationConfiguration
{
    public const string SectionName = "Authentication";

    [Required]
    [Url]
    public required string MetadataAddress { get; init; }

    [Required]
    public required string IssuerAddress { get; init; }

    [Required]
    public required string AudienceAddress { get; init; }

    [Required]
    public required string ClientId { get; init; }

    public string? ClientSecret { get; init; }

    [Required]
    [Url]
    public required string AuthorizationEndpoint { get; init; }

    [Required]
    [Url]
    public required string TokenEndpoint { get; init; }

    [Required]
    [MinLength(1)]
    public required string[] Scopes { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;
}
