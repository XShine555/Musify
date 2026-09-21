namespace Musify.Application.Tracks.Responses
{
    public record TrackSearchItemResponse(TrackApplicationResponse Track)
    {
        public static TrackSearchItemResponse FromTrack(TrackApplicationResponse track) => new(track);
    }
}
