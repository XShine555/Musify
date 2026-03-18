using Ardalis.Result;
using DispatchR.Abstractions.Send;

namespace Musify.Application.Users.Commands.DeleteUser
{
    public record DeleteUserCommand(Guid Id)
        : IRequest<DeleteUserCommand, Result>;
}