using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Users.Contracts;

namespace Musify.Application.Users.Queries.GetUserById
{
    public record GetUserByIdQuery(Guid Id)
        : IRequest<GetUserByIdQuery, Task<Result<UserResponse> >>;
}