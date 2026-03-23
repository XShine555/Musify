using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
    public class KeycloakConfiguration
    {
        public const string SectionName = "Keycloak";

        [Required]
        public required string BaseUrl { get; set; }

        [Required]
        public required string Realm { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}