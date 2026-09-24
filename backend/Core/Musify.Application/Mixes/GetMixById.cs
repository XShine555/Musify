using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.Mixes.Responses;
using Musify.Application.Shared;

namespace Musify.Application.Mixes;

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

        if (mix == null)
            return AppErrors.NotFound("Mix", request.MixId);

        var orderedItems = mix.Items.OrderBy(item => item.Position).ToList();
        var items = await MixItemMapper.ToResponsesAsync(database, orderedItems, cancellationToken);

        return new MixApplicationResponse(mix.Id, mix.Kind, items.Count, items);
    }
}
