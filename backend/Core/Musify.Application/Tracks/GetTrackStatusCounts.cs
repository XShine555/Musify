using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Responses;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks
{
    public record GetTrackStatusCountsQuery(long UserId)
        : IQuery<TrackStatusCountsResponse>;

    public class GetTrackStatusCountsQueryHandler(IDatabase database)
        : IQueryHandler<GetTrackStatusCountsQuery, TrackStatusCountsResponse>
    {
        public async ValueTask<TrackStatusCountsResponse> Handle(GetTrackStatusCountsQuery request, CancellationToken cancellationToken)
        {
            var counts = await database.Tracks
                .AsNoTracking()
                .Active()
                .Where(track => track.OwnerUserId == request.UserId)
                .GroupBy(track => track.Audio.TranscodeStatus)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);

            int CountOf(ProcessingStatus status) => counts.Where(c => c.Status == status).Sum(c => c.Count);

            return new TrackStatusCountsResponse(
                counts.Sum(c => c.Count),
                CountOf(ProcessingStatus.Pending),
                CountOf(ProcessingStatus.Processing),
                CountOf(ProcessingStatus.Completed),
                CountOf(ProcessingStatus.Failed));
        }
    }
}
