using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Users.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.Users.Commands
{
    public record CreateUserCommand(Guid Id)
        : IRequest<CreateUserCommand, Task<Result<UserResponse>> >
    {
        public static User ToEntity(CreateUserCommand command)
        {
            return new User
            {
                Id = command.Id
            };
        }
    }
}