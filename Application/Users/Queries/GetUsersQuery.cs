using Ardalis.Result;
using Mediator;
using Musify.Application.Pagination;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Queries
{
    public record GetUsersQuery(int PageNumber, int PageSize, string? UsernameSearch = null)
        : IQuery<Result<PaginatedResponse<UserApplicationResponse> >>;
}
