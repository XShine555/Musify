using System.ComponentModel.DataAnnotations;
using Musify.Application.Configuration;

namespace Musify.Api.Authentication
{
    public sealed class AuthenticationConfiguration : IConfigurationOptions
    {
        public static string SectionName => "Authentication";

        [Required]
        [Url]
        public required string MetadataAddress { get; set; }

        [Required]
        public required string IssuerAddress { get; set; }

        [Required]
        public required string AudienceAddress { get; set; }

        [Required]
        public required string ClientId { get; set; }

        public string? ClientSecret { get; set; }

        [Required]
        [Url]
        public required string AuthorizationEndpoint { get; set; }

        [Required]
        [Url]
        public required string TokenEndpoint { get; set; }

        [Required]
        [MinLength(1)]
        public required string[] Scopes { get; set; }

        public string? ScalarRedirectUri { get; set; }

        public bool RequireHttpsMetadata { get; set; } = true;
    }
}
