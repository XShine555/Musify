namespace Musify.Infrastructure.MassTransit.Arguments;

internal record MarkAlbumAsRemovingArguments(
    Guid AlbumId);
