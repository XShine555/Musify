using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Tracks.Responses;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;

namespace Musify.Application.Mixes
{
    public record GenerateMixesForUserCommand(long UserId)
        : ICommand<ErrorOr<Success>>;

    public class GenerateMixesForUserCommandHandler(
        IDatabase database,
        IYouTubeMusicService youTubeMusicService,
        MixConfiguration mixConfiguration,
        ILogger<GenerateMixesForUserCommandHandler> logger)
        : ICommandHandler<GenerateMixesForUserCommand, ErrorOr<Success>>
    {
        private sealed record SeedArtist(Guid Id, string Name);

        private sealed record LocalCandidate(Guid TrackId, string Title, string? Artist, int DurationSeconds);

        private sealed record MixDraft(string Title, string? Subtitle, IReadOnlyList<MixItem> Items);

        private const int TitleLength = 300;
        private const int ArtistLength = 300;
        private const int ThumbnailUrlLength = 512;
        private const int MixTitleLength = 100;

        public async ValueTask<ErrorOr<Success>> Handle(GenerateMixesForUserCommand request, CancellationToken cancellationToken)
        {
            var userId = request.UserId;

            var historyTrackIds = await database.ListeningHistories
                .AsNoTracking()
                .Where(history => history.UserId == userId)
                .Select(history => history.TrackId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (historyTrackIds.Count == 0)
            {
                logger.LogInformation("User {UserId} has no listening history yet, skipping mix generation", userId);
                return new Success();
            }

            var seedArtists = await GetSeedArtistsAsync(historyTrackIds, cancellationToken);

            if (seedArtists.Count == 0)
            {
                logger.LogInformation("User {UserId} has no artists to seed mixes from, skipping mix generation", userId);
                return new Success();
            }

            var knownVideoIds = await database.ExternalTracks
                .AsNoTracking()
                .Where(track => track.Source == TrackSource.YouTube && historyTrackIds.Contains(track.Id))
                .Select(track => track.ExternalId)
                .ToListAsync(cancellationToken);

            var excludedVideoIds = new HashSet<string>(knownVideoIds, StringComparer.OrdinalIgnoreCase);

            var youTubeByArtist = await SearchYouTubeAsync(seedArtists, excludedVideoIds, cancellationToken);
            var localByArtist = await GetLocalCandidatesAsync(seedArtists, historyTrackIds, cancellationToken);

            var drafts = BuildDrafts(seedArtists, localByArtist, youTubeByArtist);

            if (drafts.Count == 0)
            {
                logger.LogInformation("No candidate songs found for user {UserId}, skipping mix generation", userId);
                return new Success();
            }

            await ReplaceMixesAsync(userId, drafts, cancellationToken);

            try
            {
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to generate mixes for user {UserId}", userId);
                return Error.Failure(description: $"Failed to generate mixes for user {userId}");
            }

            logger.LogInformation("Generated {Count} mixes for user {UserId}", drafts.Count, userId);
            return new Success();
        }

        private async Task<List<SeedArtist>> GetSeedArtistsAsync(
            IReadOnlyList<Guid> historyTrackIds,
            CancellationToken cancellationToken)
        {
            var ranked = await database.TrackArtists
                .AsNoTracking()
                .Where(trackArtist => historyTrackIds.Contains(trackArtist.TrackId))
                .GroupBy(trackArtist => trackArtist.ArtistId)
                .Select(group => new { ArtistId = group.Key, Listens = group.Count() })
                .OrderByDescending(entry => entry.Listens)
                .Take(mixConfiguration.SeedArtistCount)
                .ToListAsync(cancellationToken);

            var artistIds = ranked.Select(entry => entry.ArtistId).ToList();

            var names = await database.Artists
                .AsNoTracking()
                .Where(artist => artistIds.Contains(artist.Id))
                .ToDictionaryAsync(artist => artist.Id, artist => artist.Name, cancellationToken);

            return [.. ranked
                .Where(entry => names.ContainsKey(entry.ArtistId))
                .Select(entry => new SeedArtist(entry.ArtistId, names[entry.ArtistId]))];
        }

        private async Task<Dictionary<Guid, List<MixItem>>> SearchYouTubeAsync(
            IReadOnlyList<SeedArtist> seedArtists,
            HashSet<string> excludedVideoIds,
            CancellationToken cancellationToken)
        {
            var byArtist = new Dictionary<Guid, List<MixItem>>();

            foreach (var seed in seedArtists)
            {
                var searchResult = await youTubeMusicService.SearchSongsAsync(seed.Name, string.Empty, cancellationToken);
                if (searchResult.IsError)
                {
                    logger.LogWarning("YouTube Music search failed for seed artist {Artist}: {Error}",
                        seed.Name, searchResult.FirstError.Description);
                    byArtist[seed.Id] = [];
                    continue;
                }

                var songs = new List<MixItem>();
                foreach (var song in searchResult.Value.Items.Take(mixConfiguration.YouTubeResultsPerSeed))
                {
                    if (!excludedVideoIds.Add(song.VideoId))
                        continue;

                    songs.Add(new MixItem
                    {
                        MixId = Guid.Empty,
                        Source = MixItemSource.YouTube,
                        VideoId = song.VideoId,
                        Title = Truncate(song.Title, TitleLength),
                        Artist = Truncate(song.Artist, ArtistLength),
                        ThumbnailUrl = Truncate(youTubeMusicService.ResolveArtworkUrl(song.ThumbnailUrl), ThumbnailUrlLength),
                        DurationSeconds = song.DurationSeconds,
                        IsExplicit = song.IsExplicit
                    });
                }

                byArtist[seed.Id] = songs;
            }

            return byArtist;
        }

        private async Task<Dictionary<Guid, List<MixItem>>> GetLocalCandidatesAsync(
            IReadOnlyList<SeedArtist> seedArtists,
            IReadOnlyList<Guid> historyTrackIds,
            CancellationToken cancellationToken)
        {
            var artistIds = seedArtists.Select(seed => seed.Id).ToList();

            var links = await database.TrackArtists
                .AsNoTracking()
                .Where(trackArtist => artistIds.Contains(trackArtist.ArtistId) && !historyTrackIds.Contains(trackArtist.TrackId))
                .Select(trackArtist => new { trackArtist.ArtistId, trackArtist.TrackId })
                .ToListAsync(cancellationToken);

            var trackIds = links.Select(link => link.TrackId).Distinct().ToList();

            var candidates = await LoadCandidatesAsync(trackIds, cancellationToken);

            var byArtist = new Dictionary<Guid, List<MixItem>>();
            foreach (var seed in seedArtists)
            {
                byArtist[seed.Id] = [.. links
                    .Where(link => link.ArtistId == seed.Id)
                    .Select(link => candidates.GetValueOrDefault(link.TrackId))
                    .Where(candidate => candidate is not null)
                    .Select(candidate => ToMixItem(candidate!))];
            }

            return byArtist;
        }

        private async Task<Dictionary<Guid, LocalCandidate>> LoadCandidatesAsync(
            IReadOnlyList<Guid> trackIds,
            CancellationToken cancellationToken)
        {
            if (trackIds.Count == 0)
                return [];

            var tracks = await database.Tracks
                .AsNoTracking()
                .Where(track => trackIds.Contains(track.Id) && track.LifeCycleStatus == LifeCycleStatus.Active)
                .Include(track => ((ExternalTrack)track).TrackArtists)
                    .ThenInclude(trackArtist => trackArtist.Artist)
                .Include(track => ((LocalTrack)track).Owner)
                .ToListAsync(cancellationToken);

            return tracks
                .Select(track => TrackApplicationResponse.FromEntity(track, 0))
                .ToDictionary(
                    response => response.Id,
                    response => new LocalCandidate(response.Id, response.Title, response.Artist, response.Duration));
        }

        private static MixItem ToMixItem(LocalCandidate candidate) => new()
        {
            MixId = Guid.Empty,
            Source = MixItemSource.Musify,
            TrackId = candidate.TrackId,
            Title = candidate.Title,
            Artist = candidate.Artist,
            DurationSeconds = candidate.DurationSeconds
        };

        private List<MixDraft> BuildDrafts(
            IReadOnlyList<SeedArtist> seedArtists,
            IReadOnlyDictionary<Guid, List<MixItem>> localByArtist,
            IReadOnlyDictionary<Guid, List<MixItem>> youTubeByArtist)
        {
            var drafts = new List<MixDraft>();

            var everyYouTube = seedArtists.SelectMany(seed => youTubeByArtist[seed.Id]).ToList();
            var everyLocal = seedArtists.SelectMany(seed => localByArtist[seed.Id]).ToList();

            var discovery = Take(Shuffle(everyYouTube));
            if (discovery.Count > 0)
            {
                drafts.Add(new MixDraft(
                    "Descubrimiento",
                    "Canciones nuevas de YouTube Music que todavía no has escuchado.",
                    discovery));
            }

            var daily = Take(Interleave(Shuffle(everyLocal), Shuffle(everyYouTube)));
            if (daily.Count > 0)
            {
                drafts.Add(new MixDraft(
                    "Tu mezcla diaria",
                    "Lo que más escuchas, con alguna sorpresa nueva.",
                    daily));
            }

            foreach (var seed in seedArtists.Take(mixConfiguration.ArtistMixCount))
            {
                var items = Take(Interleave(Shuffle(localByArtist[seed.Id]), Shuffle(youTubeByArtist[seed.Id])));
                if (items.Count == 0)
                    continue;

                drafts.Add(new MixDraft(
                    Truncate($"Mezcla: {seed.Name}", MixTitleLength),
                    $"Canciones de {seed.Name} en Musify y en YouTube Music.",
                    items));
            }

            return drafts;
        }

        private List<MixItem> Take(IEnumerable<MixItem> items) =>
            [.. items.Take(mixConfiguration.ItemsPerMix)];

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];

        private static List<MixItem> Shuffle(IEnumerable<MixItem> items) =>
            [.. items.OrderBy(_ => Guid.NewGuid())];

        private static List<MixItem> Interleave(IReadOnlyList<MixItem> first, IReadOnlyList<MixItem> second)
        {
            var merged = new List<MixItem>(first.Count + second.Count);
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
                var previousIds = previousMixes.Select(mix => mix.Id).ToList();
                var previousItems = await database.MixItems
                    .Where(item => previousIds.Contains(item.MixId))
                    .ToListAsync(cancellationToken);

                database.MixItems.RemoveRange(previousItems);
                database.Mixes.RemoveRange(previousMixes);
            }

            for (var position = 0; position < drafts.Count; position++)
            {
                var draft = drafts[position];
                var mix = new Mix
                {
                    UserId = userId,
                    Title = draft.Title,
                    Subtitle = draft.Subtitle,
                    Position = position
                };

                await database.Mixes.AddAsync(mix, cancellationToken);

                for (var itemPosition = 0; itemPosition < draft.Items.Count; itemPosition++)
                {
                    var item = draft.Items[itemPosition];
                    await database.MixItems.AddAsync(new MixItem
                    {
                        MixId = mix.Id,
                        Position = itemPosition,
                        Source = item.Source,
                        TrackId = item.TrackId,
                        VideoId = item.VideoId,
                        Title = item.Title,
                        Artist = item.Artist,
                        ThumbnailUrl = item.ThumbnailUrl,
                        DurationSeconds = item.DurationSeconds,
                        IsExplicit = item.IsExplicit
                    }, cancellationToken);
                }
            }
        }
    }
}
