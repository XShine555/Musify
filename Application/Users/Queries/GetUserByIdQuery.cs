using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Queries
{
    public record GetUserByIdQuery(Guid Id)
        : IRequest<GetUserByIdQuery, Task<Result<UserResponse> >>;
}