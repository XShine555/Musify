using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Musify.Application.Tests.TestSupport;
using Musify.Application.Tracks;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Tracks
{
    public sealed class RecordListeningProgressCommandHandlerTests : HandlerTestBase
    {
        private RecordListeningProgressCommandHandler CreateHandler() => new(Database);

        private async Task<ListeningHistory> ReloadAsync(Guid id) =>
            await Database.ListeningHistories.AsNoTracking()
                .SingleAsync(l => l.Id == id, TestContext.Current.CancellationToken);

        private async Task<(User User, ListeningHistory Listen)> SeedListenAsync(
            double durationSeconds, DateTime listenedAt)
        {
            var user = TestEntities.User();
            var track = TestEntities.Track(user, durationSeconds: durationSeconds);
            var listen = TestEntities.ListeningHistory(user.Id, track.Id, listenedAt, isCounted: false);
            await SeedAsync(user, track, listen);
            return (user, listen);
        }

        [Fact]
        public async Task Handle_BelowThreshold_StoresSecondsWithoutCounting()
        {
            var (user, listen) = await SeedListenAsync(200, DateTime.UtcNow.AddSeconds(-60));

            var result = await CreateHandler().Handle(
                new RecordListeningProgressCommand(user.Id, listen.Id, 10), TestContext.Current.CancellationToken);

            Assert.False(result.IsError);
            var stored = await ReloadAsync(listen.Id);
            Assert.Equal(10, stored.PlayedSeconds);
            Assert.False(stored.IsCounted);
        }

        [Fact]
        public async Task Handle_ReachesThirtySeconds_MarksAsCounted()
        {
            var (user, listen) = await SeedListenAsync(200, DateTime.UtcNow.AddSeconds(-60));

            await CreateHandler().Handle(
                new RecordListeningProgressCommand(user.Id, listen.Id, 30), TestContext.Current.CancellationToken);

            Assert.True((await ReloadAsync(listen.Id)).IsCounted);
        }

        [Fact]
        public async Task Handle_ShortTrack_CountsAtHalfDuration()
        {
            var (user, listen) = await SeedListenAsync(20, DateTime.UtcNow.AddSeconds(-60));

            await CreateHandler().Handle(
                new RecordListeningProgressCommand(user.Id, listen.Id, 10), TestContext.Current.CancellationToken);

            Assert.True((await ReloadAsync(listen.Id)).IsCounted);
        }

        [Fact]
        public async Task Handle_LowerValueThanStored_KeepsHighest()
        {
            var (user, listen) = await SeedListenAsync(200, DateTime.UtcNow.AddSeconds(-60));
            var handler = CreateHandler();

            await handler.Handle(new RecordListeningProgressCommand(user.Id, listen.Id, 40), TestContext.Current.CancellationToken);
            await handler.Handle(new RecordListeningProgressCommand(user.Id, listen.Id, 15), TestContext.Current.CancellationToken);

            var stored = await ReloadAsync(listen.Id);
            Assert.Equal(40, stored.PlayedSeconds);
            Assert.True(stored.IsCounted);
        }

        [Fact]
        public async Task Handle_MoreThanElapsedClock_IsCappedToElapsedTime()
        {
            var (user, listen) = await SeedListenAsync(200, DateTime.UtcNow.AddSeconds(-20));

            await CreateHandler().Handle(
                new RecordListeningProgressCommand(user.Id, listen.Id, 150), TestContext.Current.CancellationToken);

            var stored = await ReloadAsync(listen.Id);
            Assert.InRange(stored.PlayedSeconds!.Value, 24, 27);
        }

        [Fact]
        public async Task Handle_MoreThanTrackDuration_IsCappedToDuration()
        {
            var (user, listen) = await SeedListenAsync(50, DateTime.UtcNow.AddMinutes(-30));

            await CreateHandler().Handle(
                new RecordListeningProgressCommand(user.Id, listen.Id, 900), TestContext.Current.CancellationToken);

            Assert.Equal(52, (await ReloadAsync(listen.Id)).PlayedSeconds);
        }

        [Fact]
        public async Task Handle_OtherUsersListen_ReturnsNotFound()
        {
            var (_, listen) = await SeedListenAsync(200, DateTime.UtcNow.AddSeconds(-60));

            var result = await CreateHandler().Handle(
                new RecordListeningProgressCommand(long.MaxValue, listen.Id, 10), TestContext.Current.CancellationToken);

            Assert.True(result.IsError);
            Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public async Task Handle_InvalidSeconds_ReturnsValidationError(double seconds)
        {
            var (user, listen) = await SeedListenAsync(200, DateTime.UtcNow.AddSeconds(-60));

            var result = await CreateHandler().Handle(
                new RecordListeningProgressCommand(user.Id, listen.Id, seconds), TestContext.Current.CancellationToken);

            Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        }
    }
}
