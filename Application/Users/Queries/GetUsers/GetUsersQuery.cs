using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Users.Contracts;

namespace Musify.Application.Users.Queries.GetUsers
{
    public record GetUsersQuery(int PageNumber, int PageSize, string UsernameSearch)
        : IRequest<GetUsersQuery, Task<Result<PaginatedUserResponse>> >;
}
