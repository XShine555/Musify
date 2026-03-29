using Ardalis.Result;
using Mediator;
using Musify.Application.Contracts.Infrastructure;

namespace Musify.Application.Users.Commands
{
    public class DeleteUserCommandHandler(IDatabase database)
        : ICommandHandler<DeleteUserCommand, Result>
    {
        public async ValueTask<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await database.Users.FindAsync(request.Id, cancellationToken);
            if (existingUser is null)
                return Result.NotFound();

            database.Users.Remove(existingUser);
            await database.SaveChangesAsync(cancellationToken);

            return Result.NoContent();
        }
    }
}