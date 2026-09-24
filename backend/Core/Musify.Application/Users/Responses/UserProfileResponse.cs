
namespace Musify.Application.Users.Responses;

public record UserProfileResponse(
    long Id,
    string Name,
    string? FirstName,
    string? SecondName,
    string? ProfilePictureUrl,
    DateTime CreatedAt,
    int FollowersCount,
    int FollowingCount,
    bool IsFollowing,
    bool CanViewFollowers);
