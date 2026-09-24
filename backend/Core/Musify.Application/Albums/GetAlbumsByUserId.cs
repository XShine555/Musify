using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Albums.Responses;
using Musify.Application.Contracts;
using Musify.Application.Shared;

namespace Musify.Application.Albums;

public record GetAlbumsByUserIdQuery(long UserId, int PageNumber, int PageSize)
    : IQuery<ErrorOr<PaginatedResponse<AlbumApplicationResponse>>>;

public class GetAlbumsByUserIdQueryHandler(IDatabase database)
    : IQueryHandler<GetAlbumsByUserIdQuery, ErrorOr<PaginatedResponse<AlbumApplicationResponse>>>
{
    public async ValueTask<ErrorOr<PaginatedResponse<AlbumApplicationResponse>>> Handle(GetAlbumsByUserIdQuery request, CancellationToken cancellationToken)
    {
        var userExists = await database.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == request.UserId, cancellationToken);
        if (!userExists)
            return AppErrors.NotFound("User", request.UserId);

        return await database.Albums
            .AsNoTracking()
            .Active()
            .Where(album => album.OwnerUserId == request.UserId)
            .OrderByDescending(album => album.CreatedAt)
            .ThenBy(album => album.Id)
            .SelectResponse()
            .ToPaginatedAsync(new PageRequest(request.PageNumber, request.PageSize), cancellationToken);
    }
}
