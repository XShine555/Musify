using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.PlayLists.Responses;
using Mediator;
using Musify.Application.Contracts;

namespace Musify.Application.PlayLists
{
    public record GetPlayListByIdQuery(Guid Id)
        : IQuery<ErrorOr<PlayListApplicationResponse>>;

    public class GetPlayListByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListByIdQuery, ErrorOr<PlayListApplicationResponse>>
    {
        public async ValueTask<ErrorOr<PlayListApplicationResponse>> Handle(GetPlayListByIdQuery request, CancellationToken cancellationToken)
        {
            var entry = await database.PlayLists
                .AsNoTracking()
                .Where(p => p.Id == request.Id)
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

            if (entry is null)
                return Error.NotFound();

            return PlayListApplicationResponse.FromEntity(entry.PlayList, entry.CoverTrackIds);
        }
    }
}
