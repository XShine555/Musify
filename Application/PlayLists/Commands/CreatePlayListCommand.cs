using Ardalis.Result;
using DispatchR.Abstractions.Send;
using Musify.Application.Contracts.Application;
using Musify.Application.PlayLists.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.PlayLists.Commands
{
    public record CreatePlayListCommand(
        Guid UserId,
        string Name,
        string Description,
        IFileData? Picture)
        : IRequest<CreatePlayListCommand, Task<Result<PlayListResponse>> >
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