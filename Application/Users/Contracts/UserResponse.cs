namespace Musify.Application.Users.Contracts
{
    public record UserResponse(
        string Id,
        string Name,
        string FirstName,
        string SecondName,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}