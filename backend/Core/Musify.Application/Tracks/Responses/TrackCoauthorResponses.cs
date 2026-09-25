using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks.Responses
{
    public record TrackCoauthorReference(long Id, string Name);

    public record TrackCoauthorInviteReference(long UserId, string Name, CoauthorStatus Status);
}
