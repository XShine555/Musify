using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Users.Queries;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Handlers
{
    public class GetUserByIdQueryHandler(IDatabase database, IKeycloakUserService keycloakUserClient)
        : IRequestHandler<GetUserByIdQuery, Task<Result<UserResponse> >>
    {
        public async Task<Result<UserResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await database.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
            if (user is null)
                return Result.NotFound("User not found");

            var keycloakUser = await keycloakUserClient.GetUserByIdAsync(user.Id.ToString(), cancellationToken);
            if (!keycloakUser.IsSuccess)
                return Result.NotFound("Keycloak user not found");

            return Result.Success(new UserResponse(
                user.Id.ToString(),
                keycloakUser.Value.Name,
                keycloakUser.Value.FirstName,
                keycloakUser.Value.SecondName,
                user.CreatedAt,
                user.UpdatedAt));
        }
    }
}