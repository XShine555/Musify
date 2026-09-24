using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Mixes.Responses;

namespace Musify.Application.Mixes;

public record GetMixesByUserIdQuery(long UserId)
    : IQuery<ErrorOr<IReadOnlyList<MixApplicationResponse>>>;

public class GetMixesByUserIdQueryHandler(IDatabase database)
    : IQueryHandler<GetMixesByUserIdQuery, ErrorOr<IReadOnlyList<MixApplicationResponse>>>
{
    public async ValueTask<ErrorOr<IReadOnlyList<MixApplicationResponse>>> Handle(GetMixesByUserIdQuery request, CancellationToken cancellationToken)
    {
        var mixes = await database.Mixes
            .AsNoTracking()
            .Where(mix => mix.UserId == request.UserId)
            .Include(mix => mix.Items)
            .OrderBy(mix => mix.Position)
            .ToListAsync(cancellationToken);

        var responses = new List<MixApplicationResponse>(mixes.Count);
        foreach (var mix in mixes)
        {
            var orderedItems = mix.Items.OrderBy(item => item.Position).ToList();
            var coverItems = await MixItemMapper.ToResponsesAsync(
                database, orderedItems.Take(MixApplicationResponse.CoverItemCount).ToList(), cancellationToken);

            responses.Add(new MixApplicationResponse(mix.Id, mix.Title, mix.Subtitle, orderedItems.Count, coverItems));
        }

        return responses;
    }
}
