namespace WebApi.DataTransferObjects.PlayLists;

public record UpdatePlayListRequest(
    string? NewName,
    string? NewDescription,
    Guid? NewPictureIntentId);
