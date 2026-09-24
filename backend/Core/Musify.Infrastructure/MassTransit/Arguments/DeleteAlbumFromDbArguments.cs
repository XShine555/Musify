namespace Musify.Infrastructure.MassTransit.Arguments;

internal record DeleteAlbumFromDbArguments(
    Guid AlbumId);
