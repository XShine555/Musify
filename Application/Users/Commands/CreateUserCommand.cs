using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Commands
{
    public record CreateUserCommand(Guid Id)
        : IRequest<CreateUserCommand, Task<Result<UserResponse>>>;
}