using Musify.Domain.Entities;

namespace Musify.Application.Users.Responses
{
    public record UserResponse(
        Guid Id,
        string Name,
        string? FirstName,
        string? SecondName,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public static UserResponse FromEntity(User user)
        {
            return new UserResponse(
                user.Id,
                user.Name,
                user.FirstName,
                user.SecondName,
                user.CreatedAt,
                user.UpdatedAt);
        }
    }
}