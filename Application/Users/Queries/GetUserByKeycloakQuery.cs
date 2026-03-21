using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Queries
{
    public record GetUserByKeycloakQuery(Guid Id)
        : IRequest<GetUserByKeycloakQuery, Task<Result<UserResponse> >>;
}
