
namespace Musify.Application.Users.Responses
{
    public record UserSummaryResponse(
        long Id,
        string Name,
        string? FirstName,
        string? SecondName,
        string? ProfilePictureUrl,
        bool IsFollowedByViewer);
}
