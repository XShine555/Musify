using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.PlayLists.Contracts;

namespace Musify.Application.PlayLists.Queries.GetPlayListById
{
    public class GetPlayListByIdQueryHandler(IDatabase database)
        : IRequestHandler<GetPlayListByIdQuery, Task<Result<PlayListResponse>> >
    {
        public async Task<Result<PlayListResponse>> Handle(GetPlayListByIdQuery request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            return playList is not null
                ? Result.Success(PlayListResponse.FromEntity(playList))
                : Result.NotFound("Playlist not found.");
        }
    }
}