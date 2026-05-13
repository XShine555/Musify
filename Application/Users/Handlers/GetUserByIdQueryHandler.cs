using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Abstractions.Infrastructure;
using Musify.Application.Users.Queries;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users.Handlers
{
    public class GetUserByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetUserByIdQuery, Result<UserApplicationResponse> >
    {
        public async ValueTask<Result<UserApplicationResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await database.Users
                .AsNoTracking()
                .Select(u => UserApplicationResponse.FromEntity(u))
                .SingleOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

            return user ?? Result<UserApplicationResponse>.NotFound();
        }
    }
}