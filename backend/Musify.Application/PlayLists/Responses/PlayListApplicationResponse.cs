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
                playList.Pictures.SmallName,
                playList.Pictures.MediumName,
                playList.Pictures.LargeName,
                playList.CreatedAt,
                playList.UpdatedAt);
        }
    }
}