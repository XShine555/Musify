using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Queries
{
    public record GetUsersQuery(int PageNumber, int PageSize, string UsernameSearch)
        : IRequest<GetUsersQuery, Task<Result<PaginatedResponse<UserResponse> >> >;
}
