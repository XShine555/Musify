using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Users.Queries;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Handlers
{
    public class GetUserByKeycloakQueryHandler(IDatabase database, IKeycloakUserService keycloakUserClient)
        : IRequestHandler<GetUserByKeycloakQuery, Task<Result<UserResponse> >>
    {
        public async Task<Result<UserResponse>> Handle(GetUserByKeycloakQuery request, CancellationToken cancellationToken)
        {
            var user = await database.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
            if (user is null)
                return Result.NotFound("User not found");

            KeycloakUserResponse keycloakUser;
            try
            {
                keycloakUser = await keycloakUserClient.GetUserByIdAsync(user.Id.ToString(), cancellationToken);
            }
            catch
            {
                return Result.NotFound("Keycloak user not found");
            }

            return Result.Success(new UserResponse(
                user.Id.ToString(),
                keycloakUser.Name,
                keycloakUser.FirstName,
                keycloakUser.SecondName,
                user.CreatedAt,
                user.UpdatedAt));
        }
    }
}
