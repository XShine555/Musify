using Ardalis.Result;
using Mediator;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Queries
{
    public record GetUserByKeycloakQuery(Guid Id)
        : IQuery<Result<UserApplicationResponse> >;
}
