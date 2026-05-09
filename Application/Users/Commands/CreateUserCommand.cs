using Ardalis.Result;
using Mediator;
using Musify.Application.Users.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.Users.Commands
{
    public record CreateUserCommand(Guid Id,
        string Name,
        string? FirstName,
        string? SecondName)
        : ICommand<Result<UserResponse>>
    {
        public static User ToEntity(CreateUserCommand command)
        {
            return new User
            {
                Id = command.Id,
                Name = command.Name,
                NormalizedName = command.Name.ToUpperInvariant(),
                FirstName = command.FirstName,
                SecondName = command.SecondName
            };
        }
    }
}