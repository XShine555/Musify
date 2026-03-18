namespace Musify.Application.Users.Contracts
{
    public record PaginatedUserResponse(
        IReadOnlyCollection<UserResponse> Users,
        int PageNumber,
        int PageSize,
        int PageCount,
        int TotalItemCount,
        bool HasNextPage,
        bool HasPreviousPage
    );
}