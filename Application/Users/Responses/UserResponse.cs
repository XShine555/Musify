namespace Musify.Application.Users.Responses
{
    public record UserResponse(
        string Id,
        string Name,
        string FirstName,
        string SecondName,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}