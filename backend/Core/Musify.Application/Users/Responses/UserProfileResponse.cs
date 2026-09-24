using System.Text.Json.Serialization;
using Musify.Application.Serialization;

namespace Musify.Application.Users.Responses;

public record UserProfileResponse(
    [property: JsonConverter(typeof(LongAsStringConverter))] long Id,
    string Name,
    string? FirstName,
    string? SecondName,
    string? ProfilePictureUrl,
    DateTime CreatedAt,
    int FollowersCount,
    int FollowingCount,
    bool IsFollowing,
    bool CanViewFollowers);
