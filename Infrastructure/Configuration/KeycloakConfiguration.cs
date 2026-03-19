using System.ComponentModel.DataAnnotations;

namespace Musify.Infrastructure.Configuration
{
#pragma warning disable CS8618
    public class KeycloakConfiguration
    {
        public const string SectionName = "Keycloak";

        [Required]
        public string BaseUrl { get; set; }

        [Required]
        public string Realm { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}