using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Shared;

namespace Musify.Application.PlayLists
{
    public record GetPlayListByIdQuery(Guid Id, long? RequestingUserId = null)
        : IQuery<ErrorOr<PlayListApplicationResponse>>;

    public class GetPlayListByIdQueryHandler(IDatabase database)
        : IQueryHandler<GetPlayListByIdQuery, ErrorOr<PlayListApplicationResponse>>
    {
        public async ValueTask<ErrorOr<PlayListApplicationResponse>> Handle(GetPlayListByIdQuery request, CancellationToken cancellationToken)
        {
            var playList = await database.PlayLists
                .AsNoTracking()
                .Active()
                .VisibleTo(request.RequestingUserId)
                .Where(p => p.Id == request.Id)
                .SelectResponse()
                .SingleOrDefaultAsync(cancellationToken);

            return playList == null
                ? AppErrors.NotFound("PlayList", request.Id)
                : playList;
        }
    }
}
