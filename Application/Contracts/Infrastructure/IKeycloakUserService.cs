using Musify.Application.Users.Responses;

namespace Musify.Application.Contracts.Infrastructure
{
    public interface IKeycloakUserService
    {
        Task<KeycloakUserResponse> GetUserByIdAsync(string keycloakId, CancellationToken cancellationToken);

        Task<IReadOnlyCollection<KeycloakUserResponse>> GetUsersAsync(
            CancellationToken cancellationToken,
            string search = "",
            int first = 0,
            int max = 20,
            string username = "");

        Task<int> GetUsersCountAsync(
            CancellationToken cancellationToken,
            string search = "",
            string username = "");
    }
}