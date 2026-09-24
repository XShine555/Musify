using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Shared;

namespace Musify.Application.PlayLists;

public record GetPlayListsByUserIdQuery(
    long UserId,
    string? Name,
    int PageNumber,
    int PageSize,
    long? ViewerId = null)
    : IQuery<PaginatedResponse<PlayListApplicationResponse>>;

public class GetPlayListsByUserIdQueryHandler(IDatabase database)
    : IQueryHandler<GetPlayListsByUserIdQuery, PaginatedResponse<PlayListApplicationResponse>>
{
    public async ValueTask<PaginatedResponse<PlayListApplicationResponse>> Handle(GetPlayListsByUserIdQuery request, CancellationToken cancellationToken)
    {
        var playListsQuery = database.PlayLists
            .AsNoTracking()
            .Active()
            .VisibleTo(request.ViewerId)
            .Where(playList => playList.OwnerUserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var normalizedName = TextNormalizer.Normalize(request.Name);
            playListsQuery = playListsQuery.Where(playList => playList.NormalizedName.Contains(normalizedName));
        }

        return await playListsQuery
            .OrderBy(playList => playList.CreatedAt)
            .ThenBy(playList => playList.Id)
            .SelectResponse()
            .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
    }
}
