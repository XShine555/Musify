using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Users.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.Users;

public record CreateUserCommand(long Id,
    string Name,
    string? FirstName,
    string? SecondName)
    : ICommand<ErrorOr<UserApplicationResponse>>
{
    public static User ToEntity(CreateUserCommand command)
    {
        return new User
        {
            Id = command.Id,
            Name = command.Name,
            NormalizedName = command.Name.ToUpperInvariant(),
            FirstName = command.FirstName,
            SecondName = command.SecondName
        };
    }
}

public class CreateUserCommandHandler(IDatabase database, ILogger<CreateUserCommandHandler> logger)
    : ICommandHandler<CreateUserCommand, ErrorOr<UserApplicationResponse>>
{
    public async ValueTask<ErrorOr<UserApplicationResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userExists = await database.Users.AnyAsync(u => u.Id == request.Id, cancellationToken);
        if (userExists)
        {
            logger.LogInformation("User {UserId} already exists", request.Id);
            return Error.Conflict(description: $"User {request.Id} already exists");
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
            return Error.Failure(description: $"Failed to create user {request.Id}");
        }

        logger.LogInformation("Created user {UserId}", newUser.Id);
        return UserApplicationResponse.FromEntity(newUser);
    }
}
