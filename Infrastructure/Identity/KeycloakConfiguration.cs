namespace Musify.Infrastructure.Identity
{
    public class KeycloakConfiguration
    {
        public const string SectionName = "Keycloak";

        public string BaseUrl { get; set; } = string.Empty;

        public string Realm { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}