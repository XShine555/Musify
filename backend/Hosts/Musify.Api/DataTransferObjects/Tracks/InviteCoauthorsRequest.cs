namespace Musify.Api.DataTransferObjects.Tracks
{
    public record InviteCoauthorsRequest(IReadOnlyList<long> UserIds);
}
