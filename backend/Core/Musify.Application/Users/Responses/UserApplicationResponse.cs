using Musify.Domain.Entities;

namespace Musify.Application.Users.Responses
{
    public record UserApplicationResponse(
        long Id,
        string Name,
        string? FirstName,
        string? SecondName,
        string? ProfilePictureUrl,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public static UserApplicationResponse FromEntity(User user)
        {
            return new UserApplicationResponse(
                user.Id,
                user.Name,
                user.FirstName,
                user.SecondName,
                user.ProfilePictureUrl,
                user.CreatedAt,
                user.UpdatedAt);
        }
    }
}