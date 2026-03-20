using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Contracts.Infrastructure;
using Musify.Application.Users.Contracts;
using X.PagedList;
using X.PagedList.Extensions;

namespace Musify.Application.Users.Queries.GetUsers
{
    public class GetUsersQueryHandler(IDatabase database, IKeycloakUserService keycloakUserClient)
        : IRequestHandler<GetUsersQuery, Task<Result<PaginatedUserResponse> >>
    {
        public async Task<Result<PaginatedUserResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
            var first = (pageNumber - 1) * pageSize;

            var keycloakUsers = await keycloakUserClient.GetUsersAsync(
                cancellationToken,
                first: first,
                max: pageSize,
                search: request.UsernameSearch);

            var keycloakIds = keycloakUsers.Select(u => u.Id).ToList();

            var databaseUsers = await database.Users
                .Where(u => keycloakIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            var users = databaseUsers
                .Join(keycloakUsers, db => db.Id, kc => kc.Id,
                    (databaseSource, keycloakSource) => new UserResponse(
                        keycloakSource.Id.ToString(),
                        keycloakSource.Name,
                        keycloakSource.FirstName,
                        keycloakSource.SecondName,
                        databaseSource.CreatedAt,
                        databaseSource.UpdatedAt))
                .ToArray();

            var totalItemCount = await keycloakUserClient.GetUsersCountAsync(cancellationToken, search: request.UsernameSearch);

            var pagedUsers = new StaticPagedList<UserResponse>(users, pageNumber, pageSize, totalItemCount);

            var response = new PaginatedUserResponse(
                pagedUsers.ToArray(),
                pagedUsers.PageNumber,
                pagedUsers.PageSize,
                pagedUsers.PageCount,
                pagedUsers.TotalItemCount,
                pagedUsers.HasNextPage,
                pagedUsers.HasPreviousPage);

            return Result.Success(response);
        }
    }
}
