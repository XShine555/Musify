using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.Users.Commands;
using Musify.Domain.Entities;

namespace Musify.Application.Users.Handlers
{
    public class SyncUserCommandHandler(IDatabase database, ILogger<SyncUserCommandHandler> logger)
        : ICommandHandler<SyncUserCommand, Result>
    {
        public async ValueTask<Result> Handle(SyncUserCommand request, CancellationToken cancellationToken)
        {
            var user = await database.Users
                .SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

            if (user is null)
            {
                await database.Users.AddAsync(new User
                {
                    Id = request.Id,
                    Name = request.Name,
                    NormalizedName = request.Name.ToUpperInvariant(),
                    FirstName = request.FirstName,
                    SecondName = request.SecondName,
                    ProfilePictureUrl = request.ProfilePictureUrl
                }, cancellationToken);

                await database.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Provisioned user {UserId}", request.Id);

                return Result.Success();
            }

            var hasChanges = false;

            if (user.Name != request.Name)
            {
                user.Name = request.Name;
                user.NormalizedName = request.Name.ToUpperInvariant();
                hasChanges = true;
            }

            if (user.FirstName != request.FirstName)
            {
                user.FirstName = request.FirstName;
                hasChanges = true;
            }

            if (user.SecondName != request.SecondName)
            {
                user.SecondName = request.SecondName;
                hasChanges = true;
            }

            if (user.ProfilePictureUrl != request.ProfilePictureUrl)
            {
                user.ProfilePictureUrl = request.ProfilePictureUrl;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await database.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Synced profile for user {UserId}", request.Id);
            }

            return Result.Success();
        }
    }
}
