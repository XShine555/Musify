using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Contracts;
using Musify.Application.PlayLists.Responses;
using Musify.Application.Shared;
using Musify.Domain.ValueObjects;

namespace Musify.Application.PlayLists
{
    public record UpdatePlayListCommand(
        long UserId,
        Guid PlayListId,
        string NewName,
        string? NewDescription,
        PlayListVisibility NewVisibility)
        : ICommand<ErrorOr<PlayListApplicationResponse>>;

    public class UpdatePlayListCommandHandler(
        IDatabase database,
        ILogger<UpdatePlayListCommandHandler> logger)
        : ICommandHandler<UpdatePlayListCommand, ErrorOr<PlayListApplicationResponse>>
    {
        public async ValueTask<ErrorOr<PlayListApplicationResponse>> Handle(UpdatePlayListCommand request, CancellationToken cancellationToken)
        {
            var found = await database.PlayLists.FindOwnedAsync(request.PlayListId, request.UserId, cancellationToken);
            if (found.IsError)
                return found.Errors;

            var playList = found.Value;

            playList.Name = request.NewName.Trim();
            playList.NormalizedName = TextNormalizer.Normalize(request.NewName);
            playList.Description = string.IsNullOrWhiteSpace(request.NewDescription) ? null : request.NewDescription.Trim();
            playList.Visibility = request.NewVisibility;

            await database.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated playlist {PlayListId}", playList.Id);

            return await database.PlayLists
                .AsNoTracking()
                .Where(p => p.Id == playList.Id)
                .SelectResponse()
                .SingleAsync(cancellationToken);
        }
    }
}
