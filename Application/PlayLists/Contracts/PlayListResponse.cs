namespace Musify.Application.PlayLists.Contracts
{
    public record PlayListResponse(
        Guid Id,
        string Name,
        string Description,
        string ImageName,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}