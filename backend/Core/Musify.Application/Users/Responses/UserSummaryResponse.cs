using System.Text.Json.Serialization;
using Musify.Application.Serialization;

namespace Musify.Application.Users.Responses;

public record UserSummaryResponse(
    [property: JsonConverter(typeof(LongAsStringConverter))] long Id,
    string Name,
    string? FirstName,
    string? SecondName,
    string? ProfilePictureUrl,
    bool IsFollowedByViewer);
