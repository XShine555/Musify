using ErrorOr;
using Mediator;

namespace Musify.Application.Users.Commands
{
    public record SyncUserCommand(
        long Id,
        string Name,
        string? FirstName,
        string? SecondName,
        string? ProfilePictureUrl)
        : ICommand<ErrorOr<Success>>;
}
