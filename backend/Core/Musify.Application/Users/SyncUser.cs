using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Shared;
using Musify.Domain.Entities;

namespace Musify.Application.Users;

public record SyncUserCommand(
    long Id,
    string Name,
    string? FirstName,
    string? SecondName,
    string? ProfilePictureUrl)
    : ICommand<ErrorOr<Success>>;

public class SyncUserCommandHandler(IDatabase database, ILogger<SyncUserCommandHandler> logger)
    : ICommandHandler<SyncUserCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SyncUserCommand request, CancellationToken cancellationToken)
    {
        var user = await database.Users
            .SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
        {
            await database.Users.AddAsync(new User
            {
                Id = request.Id,
                Name = request.Name,
                NormalizedName = TextNormalizer.Normalize(request.Name),
                FirstName = request.FirstName,
                SecondName = request.SecondName,
                ProfilePictureUrl = request.ProfilePictureUrl
            }, cancellationToken);

            await database.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Provisioned user {UserId}", request.Id);

            return Result.Success;
        }

        user.Name = request.Name;
        user.NormalizedName = TextNormalizer.Normalize(request.Name);
        user.FirstName = request.FirstName;
        user.SecondName = request.SecondName;
        user.ProfilePictureUrl = request.ProfilePictureUrl;

        var written = await database.SaveChangesAsync(cancellationToken);
        if (written > 0)
            logger.LogInformation("Synced profile for user {UserId}", request.Id);

        return Result.Success;
    }
}
