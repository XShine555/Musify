using Microsoft.Extensions.Configuration;

namespace Musify.Infrastructure.Identity
{
    public class KeycloakConfiguration
    {
        public const string SectionName = "Keycloak";

        public string BaseUrl { get; set; } = string.Empty;

        public string Realm { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public static KeycloakConfiguration Load(IConfiguration configuration)
        {
            var keycloakConfiguration = configuration.GetSection(SectionName).Get<KeycloakConfiguration>()
                ?? throw new InvalidOperationException($"{SectionName} configuration section not found.");

            Validate(keycloakConfiguration);

            return keycloakConfiguration;
        }

        static void Validate(KeycloakConfiguration configuration)
        {
            EnsureValue(configuration.BaseUrl, nameof(configuration.BaseUrl));
            EnsureValue(configuration.Realm, nameof(configuration.Realm));
            EnsureValue(configuration.Username, nameof(configuration.Username));
            EnsureValue(configuration.Password, nameof(configuration.Password));
        }

        static void EnsureValue(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"{name} configuration value not found.");
        }
    }
}