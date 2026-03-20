using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Users.Contracts;

namespace Musify.Application.Users.Commands.CreateUser
{
    public record CreateUserCommand(Guid Id)
        : IRequest<CreateUserCommand, Task<Result<UserResponse>>>;
}