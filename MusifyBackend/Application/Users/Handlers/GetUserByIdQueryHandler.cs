using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
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
                .Where(u => u.Id == request.Id)
                .Select(u => UserApplicationResponse.FromEntity(u))
                .SingleOrDefaultAsync(cancellationToken);

            return user is null
                ? Result<UserApplicationResponse>.NotFound()
                : Result.Success(user);
        }
    }
}