using ErrorOr;
using Mediator;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Queries
{
    public record GetUserByIdQuery(long Id)
        : IQuery<ErrorOr<UserApplicationResponse> >;
}