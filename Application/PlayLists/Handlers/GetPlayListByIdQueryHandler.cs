using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.PlayLists.Responses;
using Musify.Application.PlayLists.Queries;
using Mediator;

namespace Musify.Application.PlayLists.Handlers
{
    public class GetPlayListByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListByIdQuery, Result<PlayListResponse>>
    {
        public async ValueTask<Result<PlayListResponse>> Handle(GetPlayListByIdQuery request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists
                .AsNoTracking()
                .Select(p => PlayListResponse.FromEntity(p))
                .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            return playList ?? Result<PlayListResponse>.NotFound();
        }
    }
}