using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Users.Commands;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Handlers
{
    public class CreateUserCommandHandler(IDatabase database, IKeycloakUserService keycloakUserClient, ILogger<CreateUserCommandHandler> logger)
        : IRequestHandler<CreateUserCommand, Task<Result<UserResponse> >>
    {
        public async Task<Result<UserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users.AnyAsync(u => u.Id == request.Id, cancellationToken);
            if (userExists)
            {
                logger.LogInformation("User {UserId} already exists", request.Id);
                return Result<UserResponse>.Conflict($"User {request.Id} already exists");
            }

            var keycloakUser = await keycloakUserClient.GetUserByIdAsync(request.Id.ToString(), cancellationToken);
            if (!keycloakUser.IsSuccess)
            {
                logger.LogWarning("Keycloak user {UserId} not found", request.Id);
                return Result.NotFound("Keycloak user not found");
            }

            var newUser = CreateUserCommand.ToEntity(request);
            await database.Users.AddAsync(newUser, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created user {UserId}", newUser.Id);
            return Result.Created(new UserResponse(
                newUser.Id.ToString(),
                keycloakUser.Value.Name,
                keycloakUser.Value.FirstName,
                keycloakUser.Value.SecondName,
                newUser.CreatedAt,
                newUser.UpdatedAt));
        }
    }
}