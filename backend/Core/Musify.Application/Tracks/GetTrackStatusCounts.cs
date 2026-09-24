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
                .GroupBy(track => new { track.Audio.TranscodeStatus, track.IsPublished })
                .Select(group => new { group.Key.TranscodeStatus, group.Key.IsPublished, Count = group.Count() })
                .ToListAsync(cancellationToken);

            int CountOf(ProcessingStatus status) => counts.Where(c => c.TranscodeStatus == status).Sum(c => c.Count);

            var published = counts.Where(c => c.IsPublished).Sum(c => c.Count);
            var total = counts.Sum(c => c.Count);

            return new TrackStatusCountsResponse(
                total,
                CountOf(ProcessingStatus.Pending),
                CountOf(ProcessingStatus.Processing),
                CountOf(ProcessingStatus.Completed),
                CountOf(ProcessingStatus.Failed),
                published,
                total - published);
        }
    }
}
