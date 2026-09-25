namespace Musify.Application.Coauthors.Responses
{
    public record CoauthorInvitationResponse(
        Guid Id,
        Guid TrackId,
        string TrackTitle,
        string? Artist,
        long OwnerUserId,
        DateTime InvitedAt);
}
