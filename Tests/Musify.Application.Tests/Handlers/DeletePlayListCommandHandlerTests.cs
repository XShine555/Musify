using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Application.Contracts;
using Musify.Application.Events;
using Musify.Application.PlayLists.Commands;
using Musify.Application.PlayLists.Handlers;
using Musify.Application.Tests.Infrastructure;
using Musify.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace Musify.Application.Tests.Handlers;

public sealed class DeletePlayListCommandHandlerTests
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private DeletePlayListCommandHandler CreateSut(TestDatabase db) =>
        new(db, _eventBus, NullLogger<DeletePlayListCommandHandler>.Instance);

    [Fact]
    public async Task When_playlist_missing_returns_not_found()
    {
        await using var ctx = new SqliteTestContext();
        var sut = CreateSut(ctx.Database);

        var result = await sut.Handle(new DeletePlayListCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task When_user_not_owner_returns_unauthorized()
    {
        await using var ctx = new SqliteTestContext();
        var playList = TestData.PlayList(Guid.NewGuid());
        ctx.Database.PlayLists.Add(playList);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(new DeletePlayListCommand(Guid.NewGuid(), playList.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Unauthorized);
    }

    [Fact]
    public async Task Happy_path_marks_removing_publishes_event_and_returns_no_content()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var playList = TestData.PlayList(userId);
        ctx.Database.PlayLists.Add(playList);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(new DeletePlayListCommand(userId, playList.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NoContent);
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<DeletePlayListEvent>(e => e.PlayListId == playList.Id && e.UserId == userId),
            Arg.Any<CancellationToken>());

        var persisted = await ctx.NewContext().PlayLists.SingleAsync(p => p.Id == playList.Id);
        persisted.LifeCycleStatus.Should().Be(LifeCycleStatus.Removing);
    }

    [Fact]
    public async Task When_publish_throws_returns_error_and_does_not_persist()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var playList = TestData.PlayList(userId);
        ctx.Database.PlayLists.Add(playList);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        _eventBus.PublishAsync(Arg.Any<DeletePlayListEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("bus down"));

        var sut = CreateSut(ctx.Database);
        var result = await sut.Handle(new DeletePlayListCommand(userId, playList.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Error);
        var persisted = await ctx.NewContext().PlayLists.SingleAsync(p => p.Id == playList.Id);
        persisted.LifeCycleStatus.Should().Be(LifeCycleStatus.Active);
    }
}
