using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.Tests.Infrastructure;
using Musify.Application.Tracks.Commands;
using Musify.Application.Tracks.Handlers;
using Musify.Domain.Entities;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Handlers;

public sealed class DeleteTrackCommandHandlerTests
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private DeleteTrackCommandHandler CreateSut(TestDatabase db) =>
        new(db, _eventBus, NullLogger<DeleteTrackCommandHandler>.Instance);

    private static async Task<(Guid trackId, Guid userId)> SeedOwnedTrackAsync(
        SqliteTestContext ctx, ProcessingStatus audioStatus)
    {
        var userId = Guid.NewGuid();
        var track = TestData.Track(audioStatus: audioStatus);
        ctx.Database.Tracks.Add(track);
        ctx.Database.UserHasTracks.Add(new UserHasTrack { UserId = userId, TrackId = track.Id });
        await ctx.Database.SaveChangesAsync(CancellationToken.None);
        return (track.Id, userId);
    }

    [Fact]
    public async Task When_track_missing_returns_not_found()
    {
        await using var ctx = new SqliteTestContext();
        var sut = CreateSut(ctx.Database);

        var result = await sut.Handle(new DeleteTrackCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task When_user_not_owner_returns_unauthorized()
    {
        await using var ctx = new SqliteTestContext();
        var track = TestData.Track();
        ctx.Database.Tracks.Add(track);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(new DeleteTrackCommand(Guid.NewGuid(), track.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Unauthorized);
    }

    [Theory]
    [InlineData(ProcessingStatus.Pending)]
    [InlineData(ProcessingStatus.Processing)]
    public async Task When_audio_still_processing_returns_conflict(ProcessingStatus status)
    {
        await using var ctx = new SqliteTestContext();
        var (trackId, userId) = await SeedOwnedTrackAsync(ctx, status);

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(new DeleteTrackCommand(userId, trackId), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Conflict);
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<DeleteTrackEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Happy_path_marks_removing_publishes_event_and_returns_no_content()
    {
        await using var ctx = new SqliteTestContext();
        var (trackId, userId) = await SeedOwnedTrackAsync(ctx, ProcessingStatus.Completed);

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(new DeleteTrackCommand(userId, trackId), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NoContent);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<DeleteTrackEvent>(e => e.TrackId == trackId), Arg.Any<CancellationToken>());

        var persisted = await ctx.NewContext().Tracks.SingleAsync(t => t.Id == trackId);
        persisted.LifeCycleStatus.Should().Be(LifeCycleStatus.Removing);
    }

    [Fact]
    public async Task When_publish_throws_returns_error_and_does_not_persist()
    {
        await using var ctx = new SqliteTestContext();
        var (trackId, userId) = await SeedOwnedTrackAsync(ctx, ProcessingStatus.Completed);

        _eventBus.PublishAsync(Arg.Any<DeleteTrackEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("bus down"));

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(new DeleteTrackCommand(userId, trackId), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Error);
        var persisted = await ctx.NewContext().Tracks.SingleAsync(t => t.Id == trackId);
        persisted.LifeCycleStatus.Should().Be(LifeCycleStatus.Active);
    }
}
