using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Contracts
{
    public record PlayListResponse(
        Guid Id,
        string Name,
        string Description,
        string SmallImageKeyName,
        string MediumImageKeyName,
        string LargeImageKeyName,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public static PlayListResponse FromEntity(PlayList playList)
        {
            return new PlayListResponse(
                playList.Id,
                playList.Name,
                playList.Description,
                playList.SmallPictureKeyName,
                playList.MediumPictureKeyName,
                playList.LargePictureKeyName,
                playList.CreatedDate,
                playList.UpdatedDate);
        }
    }
}