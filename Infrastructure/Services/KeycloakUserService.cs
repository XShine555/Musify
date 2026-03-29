using Keycloak.Net;
using Keycloak.Net.Models.Users;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Users.Responses;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Services
{
    public class KeycloakUserService(
        KeycloakClient keycloakClient,
        KeycloakConfiguration keycloakConfiguration,
        ILogger<KeycloakUserService> logger) : IKeycloakUserService
    {
        public async Task<KeycloakUserResponse> GetUserByIdAsync(string keycloakId, CancellationToken cancellationToken)
        {
            User keycloakUser;

            try
            {
                keycloakUser = await keycloakClient.GetUserAsync(keycloakConfiguration.Realm, keycloakId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Operation to retrieve Keycloak user {KeycloakId} was cancelled", keycloakId);
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to retrieve Keycloak user {KeycloakId}", keycloakId);
                throw;
            }
            return ToResponse(keycloakUser);
        }

        public async Task<IReadOnlyCollection<KeycloakUserResponse>> GetUsersAsync(CancellationToken cancellationToken, string search = "", int first = 0, int max = 20, string username = "")
        {
            var users = await keycloakClient.GetUsersAsync(
                realm: keycloakConfiguration.Realm,
                first: first,
                max: max,
                search: search,
                username: username,
                cancellationToken: cancellationToken);

            return users
                .Select(ToResponse)
                .ToArray();
        }

        public Task<int> GetUsersCountAsync(CancellationToken cancellationToken, string search = "", string username = "")
        {
            return keycloakClient.GetUsersCountAsync(
                keycloakConfiguration.Realm,
                search: search,
                username: username,
                cancellationToken: cancellationToken);
        }

        KeycloakUserResponse ToResponse(User keycloakUser)
        {
            return new KeycloakUserResponse(
                    Guid.Parse(keycloakUser.Id ?? throw new InvalidOperationException("Keycloak user Id is required.")),
                    keycloakUser.UserName ?? throw new InvalidOperationException("Keycloak user Name is required."),
                    keycloakUser.FirstName ?? throw new InvalidOperationException("Keycloak user FirstName is required."),
                    keycloakUser.LastName ?? throw new InvalidOperationException("Keycloak user SecondName is required."));
        }
    }
}