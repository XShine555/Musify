using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Musify.Application.PlayLists.Responses;
using Musify.Application.PlayLists.Queries;
using Mediator;
using Musify.Application.Contracts;

namespace Musify.Application.PlayLists.Handlers
{
    public class GetPlayListByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListByIdQuery, Result<PlayListApplicationResponse>>
    {
        public async ValueTask<Result<PlayListApplicationResponse>> Handle(GetPlayListByIdQuery request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists
                .AsNoTracking()
                .Select(p => PlayListApplicationResponse.FromEntity(p))
                .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            return playList ?? Result<PlayListApplicationResponse>.NotFound();
        }
    }
}