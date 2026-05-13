using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Responses
{
    public record PlayListApplicationResponse(
        Guid Id,
        string Name,
        string Description,
        string SmallImageKeyName,
        string MediumImageKeyName,
        string LargeImageKeyName,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public static PlayListApplicationResponse FromEntity(PlayList playList)
        {
            return new PlayListApplicationResponse(
                playList.Id,
                playList.Name,
                playList.Description,
                playList.SmallPictureName,
                playList.MediumPictureName,
                playList.LargePictureName,
                playList.CreatedDate,
                playList.UpdatedDate);
        }
    }
}