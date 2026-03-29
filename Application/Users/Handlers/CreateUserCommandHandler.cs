using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Users.Commands;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Handlers
{
    public class CreateUserCommandHandler(IDatabase database, IKeycloakUserService keycloakUserClient, ILogger<CreateUserCommandHandler> logger)
        : ICommandHandler<CreateUserCommand, Result<UserResponse> >
    {
        public async ValueTask<Result<UserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users.AnyAsync(u => u.Id == request.Id, cancellationToken);
            if (userExists)
            {
                logger.LogInformation("User {UserId} already exists", request.Id);
                return Result<UserResponse>.Conflict($"User {request.Id} already exists");
            }

            KeycloakUserResponse keycloakUser;
            try
            {
                keycloakUser = await keycloakUserClient.GetUserByIdAsync(request.Id.ToString(), cancellationToken);
            }
            catch
            {
                return Result.NotFound("Keycloak user not found");
            }

            var newUser = CreateUserCommand.ToEntity(request);
            await database.Users.AddAsync(newUser, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created user {UserId}", newUser.Id);
            return Result.Created(new UserResponse(
                newUser.Id.ToString(),
                keycloakUser.Name,
                keycloakUser.FirstName,
                keycloakUser.SecondName,
                newUser.CreatedAt,
                newUser.UpdatedAt));
        }
    }
}