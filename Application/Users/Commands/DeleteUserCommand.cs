using Ardalis.Result;
using DispatchR.Abstractions.Send;

namespace Musify.Application.Users.Commands
{
    public record DeleteUserCommand(Guid Id)
        : IRequest<DeleteUserCommand, Task<Result>>;
}