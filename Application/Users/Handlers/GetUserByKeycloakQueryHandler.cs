using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Users.Queries;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Handlers
{
    public class GetUserByKeycloakQueryHandler(IDatabase database)
        : IQueryHandler<GetUserByKeycloakQuery, Result<UserApplicationResponse> >
    {
        public async ValueTask<Result<UserApplicationResponse>> Handle(GetUserByKeycloakQuery request, CancellationToken cancellationToken)
        {
            var user = await database.Users
                .AsNoTracking()
                .Select(u => UserApplicationResponse.FromEntity(u))
                .SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

            return user ?? Result<UserApplicationResponse>.NotFound();
        }
    }
}
