using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Users.Commands;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Handlers
{
    public class CreateUserCommandHandler(IDatabase database, ILogger<CreateUserCommandHandler> logger)
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

            var newUser = CreateUserCommand.ToEntity(request);
            await database.Users.AddAsync(newUser, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created user {UserId}", newUser.Id);
            return Result.Created(UserResponse.FromEntity(newUser));
        }
    }
}