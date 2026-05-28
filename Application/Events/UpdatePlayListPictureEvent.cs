using Musify.Application.Common;

﻿namespace Musify.Application.Events
{
    public record UpdatePlayListPictureEvent(
        Guid PlayListId,
        string Bucket,
        string SourceKey,
        ImageSize Small,
        ImageSize Medium,
        ImageSize Large);
}