using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Users.Contracts;
using Musify.Domain.Entities;

namespace Musify.Application.Users.Commands.CreateUser
{
    public class CreateUserCommandHandler(IDatabase database, IKeycloakUserService keycloakUserClient, ILogger<CreateUserCommandHandler> logger)
        : IRequestHandler<CreateUserCommand, Task<Result<UserResponse> >>
    {
        public async Task<Result<UserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            logger.LogDebug("Handling {CommandName} with Id: {UserId}", nameof(CreateUserCommand), request.Id);

            var existingUser = await database.Users.SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

            if (existingUser is not null)
            {
                logger.LogInformation("User with Id={UserId} already exists.", request.Id);
                return Result<UserResponse>.Conflict($"A user with Id {request.Id} already exists.");
            }

            var keycloakUser = await keycloakUserClient.GetUserByIdAsync(request.Id.ToString(), cancellationToken);

            if (!keycloakUser.IsSuccess)
            {
                logger.LogWarning("Keycloak user with Id={UserId} was not found.", request.Id);
                return Result.NotFound("Keycloak user not found.");
            }

            var newUser = new User
            {
                Id = request.Id
            };

            await database.Users.AddAsync(newUser, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User with Id={UserId} created successfully.", newUser.Id);
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