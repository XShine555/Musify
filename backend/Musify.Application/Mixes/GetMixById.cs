using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Mixes.Responses;

namespace Musify.Application.Mixes
{
    public record GetMixByIdQuery(long UserId, Guid MixId)
        : IQuery<ErrorOr<MixApplicationResponse>>;

    public class GetMixByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetMixByIdQuery, ErrorOr<MixApplicationResponse>>
    {
        public async ValueTask<ErrorOr<MixApplicationResponse>> Handle(GetMixByIdQuery request, CancellationToken cancellationToken)
        {
            var mix = await database.Mixes
                .AsNoTracking()
                .Include(mix => mix.Items)
                .SingleOrDefaultAsync(mix => mix.Id == request.MixId && mix.UserId == request.UserId, cancellationToken);

            if (mix is null)
                return Error.NotFound(description: $"Mix {request.MixId} was not found");

            var items = mix.Items.OrderBy(item => item.Position).ToList();

            return MixApplicationResponse.FromEntity(mix, items.Count, items);
        }
    }
}
