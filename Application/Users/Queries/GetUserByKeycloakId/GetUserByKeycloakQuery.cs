using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Users.Contracts;

namespace Musify.Application.Users.Queries.GetUserByKeycloakId
{
    public record GetUserByKeycloakQuery(Guid Id)
        : IRequest<GetUserByKeycloakQuery, Task<Result<UserResponse> >>;
}
