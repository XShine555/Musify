using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Users.Responses;

namespace Musify.Application.Users
{
    public record GetUserByIdQuery(long Id)
        : IQuery<ErrorOr<UserApplicationResponse> >;

    public class GetUserByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetUserByIdQuery, ErrorOr<UserApplicationResponse> >
    {
        public async ValueTask<ErrorOr<UserApplicationResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await database.Users
                .AsNoTracking()
                .Where(u => u.Id == request.Id)
                .Select(u => UserApplicationResponse.FromEntity(u))
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
                return Error.NotFound();

            return user;
        }
    }
}
