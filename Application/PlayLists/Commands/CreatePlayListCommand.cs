using Ardalis.Result;
using Mediator;
using Musify.Application.Abstractions.Application;
using Musify.Application.PlayLists.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Commands
{
    public record CreatePlayListCommand(
        Guid UserId,
        string Name,
        string Description,
        IFileData? Picture)
        : ICommand<Result<PlayListApplicationResponse>>
    {
        public static PlayList ToEntity(CreatePlayListCommand command, string originalKey, string smallKey, string mediumKey, string largeKey)
        {
            return new PlayList
            {
                UserId = command.UserId,
                Name = command.Name,
                NormalizedName = command.Name.Trim().ToUpperInvariant(),
                Description = command.Description,
                OriginalPictureName = originalKey,
                SmallPictureName = smallKey,
                MediumPictureName = mediumKey,
                LargePictureName = largeKey,
            };
        }
    }
}