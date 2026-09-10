using Musify.Application.Contracts;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Tracks.Responses
{
    /// <summary>One item of a combined local/YouTube track listing. Exactly one of
    /// <see cref="Track"/> or <see cref="YouTubeSong"/> is populated: <see cref="Track"/> for
    /// anything already in the catalog (local uploads and YouTube tracks provisioned earlier),
    /// <see cref="YouTubeSong"/> for a live YouTube Music search hit that has not been
    /// provisioned yet, so it has no track id.</summary>
    public record TrackSearchItemResponse(
        TrackSource Source,
        TrackApplicationResponse? Track,
        YouTubeSongResult? YouTubeSong)
    {
        public static TrackSearchItemResponse FromTrack(TrackApplicationResponse track) =>
            new(track.Source, track, null);

        public static TrackSearchItemResponse FromYouTubeSong(YouTubeSongResult song) =>
            new(TrackSource.YouTube, null, song);
    }
}
