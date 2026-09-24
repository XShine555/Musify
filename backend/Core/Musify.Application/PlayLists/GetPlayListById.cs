using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.PlayLists.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists;

public record GetPlayListByIdQuery(Guid Id, long? RequestingUserId = null)
    : IQuery<ErrorOr<PlayListApplicationResponse>>;

public class GetPlayListByIdQueryHandler(IDatabase database)
    : IQueryHandler<GetPlayListByIdQuery, ErrorOr<PlayListApplicationResponse>>
{
    public async ValueTask<ErrorOr<PlayListApplicationResponse>> Handle(GetPlayListByIdQuery request, CancellationToken cancellationToken)
    {
        var entry = await database.PlayLists
            .AsNoTracking()
            .Where(p => p.Id == request.Id && p.LifeCycleStatus == LifeCycleStatus.Active)
            .Select(p => new
            {
                PlayList = p,
                CoverTrackIds = p.PlayListTracks
                    .OrderBy(plt => plt.Position)
                    .Take(PlayListApplicationResponse.CoverTrackCount)
                    .Select(plt => plt.TrackId)
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (entry == null)
            return Error.NotFound();

        if (entry.PlayList.Visibility == PlaylistVisibility.Private
            && entry.PlayList.UserId != request.RequestingUserId)
        {
            return Error.NotFound();
        }

        return PlayListApplicationResponse.FromEntity(entry.PlayList, entry.CoverTrackIds);
    }
}
