using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Mixes;

public record GenerateMixesForUserCommand(long UserId)
    : ICommand;

public class GenerateMixesForUserCommandHandler(
    IDatabase database,
    MixConfiguration mixConfiguration,
    ILogger<GenerateMixesForUserCommandHandler> logger)
    : ICommandHandler<GenerateMixesForUserCommand>
{
    private sealed record MixDraft(MixKind Kind, IReadOnlyList<Guid> TrackIds);

    public async ValueTask<Unit> Handle(GenerateMixesForUserCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        var libraryTrackIds = await database.Tracks
            .AsNoTracking()
            .Where(track => track.OwnerUserId == userId && track.LifeCycleStatus == LifeCycleStatus.Active)
            .Select(track => track.Id)
            .ToListAsync(cancellationToken);

        if (libraryTrackIds.Count == 0)
        {
            logger.LogInformation("User {UserId} has no tracks yet, skipping mix generation", userId);
            return Unit.Value;
        }

        var historyTrackIds = await database.ListeningHistories
            .AsNoTracking()
            .Where(history => history.UserId == userId && history.IsCounted)
            .Select(history => history.TrackId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var historySet = new HashSet<Guid>(historyTrackIds);
        var unheardTrackIds = libraryTrackIds.Where(id => !historySet.Contains(id)).ToList();

        var drafts = new List<MixDraft>();

        var discovery = Take(Shuffle(unheardTrackIds));
        if (discovery.Count > 0)
        {
            drafts.Add(new MixDraft(MixKind.Discovery, discovery));
        }

        var daily = Take(Interleave(Shuffle(historyTrackIds), Shuffle(unheardTrackIds)));
        if (daily.Count > 0)
        {
            drafts.Add(new MixDraft(MixKind.Daily, daily));
        }

        if (drafts.Count == 0)
        {
            logger.LogInformation("No candidate songs found for user {UserId}, skipping mix generation", userId);
            return Unit.Value;
        }

        await ReplaceMixesAsync(userId, drafts, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Generated {Count} mixes for user {UserId}", drafts.Count, userId);
        return Unit.Value;
    }

    private List<Guid> Take(IEnumerable<Guid> trackIds) =>
        [.. trackIds.Take(mixConfiguration.ItemsPerMix)];

    private static List<Guid> Shuffle(IEnumerable<Guid> trackIds) =>
        [.. trackIds.OrderBy(_ => Guid.NewGuid())];

    private static List<Guid> Interleave(List<Guid> first, List<Guid> second)
    {
        var merged = new List<Guid>(first.Count + second.Count);
        for (var index = 0; index < Math.Max(first.Count, second.Count); index++)
        {
            if (index < first.Count)
                merged.Add(first[index]);
            if (index < second.Count)
                merged.Add(second[index]);
        }

        return merged;
    }

    private async Task ReplaceMixesAsync(long userId, IReadOnlyList<MixDraft> drafts, CancellationToken cancellationToken)
    {
        var previousMixes = await database.Mixes
            .Where(mix => mix.UserId == userId)
            .ToListAsync(cancellationToken);

        if (previousMixes.Count > 0)
        {
            database.Mixes.RemoveRange(previousMixes);
        }

        for (var position = 0; position < drafts.Count; position++)
        {
            var draft = drafts[position];
            var mix = new Mix
            {
                UserId = userId,
                Kind = draft.Kind,
                Position = position
            };

            await database.Mixes.AddAsync(mix, cancellationToken);

            for (var itemPosition = 0; itemPosition < draft.TrackIds.Count; itemPosition++)
            {
                await database.MixItems.AddAsync(new MixItem
                {
                    MixId = mix.Id,
                    Position = itemPosition,
                    TrackId = draft.TrackIds[itemPosition]
                }, cancellationToken);
            }
        }
    }
}
