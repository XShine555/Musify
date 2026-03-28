using Ardalis.Result;
using Keycloak.Net;
using Keycloak.Net.Models.Users;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Users.Responses;
using Musify.Infrastructure.Configuration;

namespace Musify.Infrastructure.Identity
{
    public class KeycloakUserService(
        KeycloakClient keycloakClient,
        KeycloakConfiguration keycloakConfiguration,
        ILogger<KeycloakUserService> logger) : IKeycloakUserService
    {
        public async Task<Result<KeycloakUserResponse>> GetUserByIdAsync(string keycloakId, CancellationToken cancellationToken)
        {
            User keycloakUser;

            try
            {
                keycloakUser = await keycloakClient.GetUserAsync(keycloakConfiguration.Realm, keycloakId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Operation to retrieve Keycloak user {KeycloakId} was cancelled", keycloakId);
                return Result.Error("Operation cancelled");
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to retrieve Keycloak user {KeycloakId}", keycloakId);
                return Result.NotFound();
            }
            return Result.Success(KeycloakUserMapper.Map(keycloakUser));
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
                .Select(KeycloakUserMapper.Map)
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
    }
}