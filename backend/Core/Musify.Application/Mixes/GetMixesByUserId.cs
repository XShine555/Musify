using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Mixes.Responses;

namespace Musify.Application.Mixes
{
    public record GetMixesByUserIdQuery(long UserId)
        : IQuery<IReadOnlyList<MixApplicationResponse>>;

    public class GetMixesByUserIdQueryHandler(IDatabase database)
        : IQueryHandler<GetMixesByUserIdQuery, IReadOnlyList<MixApplicationResponse>>
    {
        public async ValueTask<IReadOnlyList<MixApplicationResponse>> Handle(GetMixesByUserIdQuery request, CancellationToken cancellationToken)
        {
            var mixes = await database.Mixes
                .AsNoTracking()
                .Where(mix => mix.UserId == request.UserId)
                .Include(mix => mix.Items)
                .OrderBy(mix => mix.Position)
                .ThenBy(mix => mix.Id)
                .ToListAsync(cancellationToken);

            var orderedItemsByMix = mixes.ToDictionary(
                mix => mix.Id,
                mix => mix.Items.OrderBy(item => item.Position).ToList());

            var coverItems = orderedItemsByMix.Values
                .SelectMany(items => items.Take(MixApplicationResponse.CoverItemCount))
                .ToList();
            var coverResponses = (await MixItemMapper.ToResponsesAsync(database, coverItems, cancellationToken))
                .GroupBy(response => response.TrackId)
                .ToDictionary(group => group.Key, group => group.First());

            var responses = new List<MixApplicationResponse>(mixes.Count);
            foreach (var mix in mixes)
            {
                var orderedItems = orderedItemsByMix[mix.Id];
                var covers = orderedItems
                    .Take(MixApplicationResponse.CoverItemCount)
                    .Where(item => coverResponses.ContainsKey(item.TrackId))
                    .Select(item => coverResponses[item.TrackId])
                    .ToList();

                responses.Add(new MixApplicationResponse(mix.Id, mix.Kind, orderedItems.Count, covers));
            }

            return responses;
        }
    }
}
