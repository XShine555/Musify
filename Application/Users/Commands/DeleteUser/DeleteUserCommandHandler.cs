using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Infrastructure;

namespace Musify.Application.Users.Commands.DeleteUser
{
    public class DeleteUserCommandHandler(IDatabase database)
        : IRequestHandler<DeleteUserCommand, Task<Result>>
    {
        public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
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