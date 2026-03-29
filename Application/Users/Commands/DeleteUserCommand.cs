using Ardalis.Result;
using Mediator;

namespace Musify.Application.Users.Commands
{
    public record DeleteUserCommand(Guid Id)
        : ICommand<Result>;
}