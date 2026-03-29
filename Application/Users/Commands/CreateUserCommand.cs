using Ardalis.Result;
using Mediator;
using Musify.Application.Users.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.Users.Commands
{
    public record CreateUserCommand(Guid Id)
        : ICommand<Result<UserResponse>>
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