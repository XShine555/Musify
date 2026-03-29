using Ardalis.Result;
using Mediator;
using Musify.Application.Contracts.Application;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;

namespace Musify.Application.Tracks.Commands
{
    public record CreateTrackCommand(
        Guid UserId,
        string Title,
        IFileData Picture,
        IFileData Audio)
        : ICommand<Result<TrackResponse> >
    {
        public static Track ToEntity(CreateTrackCommand command, string smallPictureName, string mediumPictureName, string largePictureName)
        {
            return new Track
            {
                Title = command.Title,
                NormalizedTitle = command.Title.ToUpperInvariant(),
                OriginalPictureName = Guid.NewGuid() + command.Picture.FileType,
                OriginalAudioName = Guid.NewGuid() + command.Audio.FileType,
                SmallPictureName = smallPictureName,
                MediumPictureName = mediumPictureName,
                LargePictureName = largePictureName
            };
        }
    }
}