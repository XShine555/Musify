using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Users.Commands;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Handlers
{
    public class CreateUserCommandHandler(IDatabase database, ILogger<CreateUserCommandHandler> logger)
        : ICommandHandler<CreateUserCommand, Result<UserApplicationResponse> >
    {
        public async ValueTask<Result<UserApplicationResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var userExists = await database.Users.AnyAsync(u => u.Id == request.Id, cancellationToken);
            if (userExists)
            {
                logger.LogInformation("User {UserId} already exists", request.Id);
                return Result<UserApplicationResponse>.Conflict($"User {request.Id} already exists");
            }

            var newUser = CreateUserCommand.ToEntity(request);

            try
            {
                await database.Users.AddAsync(newUser, cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to create user {UserId}", request.Id);
                return Result.Error($"Failed to create user {request.Id}");
            }

            logger.LogInformation("Created user {UserId}", newUser.Id);
            return Result.Created(UserApplicationResponse.FromEntity(newUser));
        }
    }
}