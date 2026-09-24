namespace Musify.Application.Events;

public record DeletePlayListEvent(
    Guid PlayListId,
    long UserId);
