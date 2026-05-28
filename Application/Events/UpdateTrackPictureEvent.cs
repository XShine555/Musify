using Musify.Application.Common;

﻿namespace Musify.Application.Events
{
    public record UpdateTrackPictureEvent(
        Guid TrackId,
        string Bucket,
        string SourceKey,
        ImageSize Small,
        ImageSize Medium,
        ImageSize Large);
}