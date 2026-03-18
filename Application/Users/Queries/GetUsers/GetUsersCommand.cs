using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Users.Contracts;

namespace Musify.Application.Users.Queries.GetUsers
{
    public record GetUsersCommand(int PageNumber, int PageSize, string UsernameSearch)
        : IRequest<GetUsersCommand, Task<Result<PaginatedUserResponse>> >;
}