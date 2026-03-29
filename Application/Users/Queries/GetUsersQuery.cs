using Ardalis.Result;
using Mediator;
using Musify.Application.Contracts.Application;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Queries
{
    public record GetUsersQuery(int PageNumber, int PageSize, string UsernameSearch)
        : IQuery<Result<PaginatedResponse<UserResponse> >>;
}
