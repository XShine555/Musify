using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.PlayLists.Responses;
using Musify.Application.PlayLists.Queries;
using Mediator;
using Musify.Application.Contracts;

namespace Musify.Application.PlayLists.Handlers
{
    public class GetPlayListByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListByIdQuery, ErrorOr<PlayListApplicationResponse>>
    {
        public async ValueTask<ErrorOr<PlayListApplicationResponse>> Handle(GetPlayListByIdQuery request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists
                .AsNoTracking()
                .Where(p => p.Id == request.Id)
                .Select(p => PlayListApplicationResponse.FromEntity(p))
                .SingleOrDefaultAsync(cancellationToken);

            if (playList is null)
                return Error.NotFound();

            return playList;
        }
    }
}