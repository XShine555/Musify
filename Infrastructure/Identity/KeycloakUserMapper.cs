using Keycloak.Net.Models.Users;
using Musify.Application.Users.Contracts;

namespace Musify.Infrastructure.Identity
{
    public static class KeycloakUserMapper
    {
        public static KeycloakUserResponse Map(User keycloakUser)
        {
            return new KeycloakUserResponse(
                Guid.Parse(keycloakUser.Id ?? throw new InvalidOperationException("Keycloak user Id is required.")),
                keycloakUser.UserName ?? throw new InvalidOperationException("Keycloak user Name is required."),
                keycloakUser.FirstName ?? throw new InvalidOperationException("Keycloak user FirstName is required."),
                keycloakUser.LastName ?? throw new InvalidOperationException("Keycloak user SecondName is required."));
        }
    }
}