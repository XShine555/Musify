namespace WebApi.DataTransferObjects.PlayLists;

public record CreatePlayListRequest(
    string Name,
    string Description,
    Guid? PictureIntentId);
