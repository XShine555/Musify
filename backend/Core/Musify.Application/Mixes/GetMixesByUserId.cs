using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Mixes.Responses;

namespace Musify.Application.Mixes
{
    public record GetMixesByUserIdQuery(long UserId)
        : IQuery<ErrorOr<IReadOnlyList<MixApplicationResponse>>>;

    public class GetMixesByUserIdQueryHandler(IDatabase database)
        : IQueryHandler<GetMixesByUserIdQuery, ErrorOr<IReadOnlyList<MixApplicationResponse>>>
    {
        public async ValueTask<ErrorOr<IReadOnlyList<MixApplicationResponse>>> Handle(GetMixesByUserIdQuery request, CancellationToken cancellationToken)
        {
            var entries = await database.Mixes
                .AsNoTracking()
                .Where(mix => mix.UserId == request.UserId)
                .OrderBy(mix => mix.Position)
                .Select(mix => new
                {
                    Mix = mix,
                    ItemCount = mix.Items.Count,
                    CoverItems = mix.Items
                        .OrderBy(item => item.Position)
                        .Take(MixApplicationResponse.CoverItemCount)
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            return entries
                .Select(entry => MixApplicationResponse.FromEntity(entry.Mix, entry.ItemCount, entry.CoverItems))
                .ToList();
        }
    }
}
