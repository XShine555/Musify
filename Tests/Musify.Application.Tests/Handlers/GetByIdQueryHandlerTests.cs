using Ardalis.Result;
using FluentAssertions;
using Musify.Application.PlayLists.Handlers;
using Musify.Application.PlayLists.Queries;
using Musify.Application.Tests.Infrastructure;
using Musify.Application.Tracks.Handlers;
using Musify.Application.Tracks.Queries;
using Musify.Application.Users.Handlers;
using Musify.Application.Users.Queries;
using Xunit;

namespace Musify.Application.Tests.Handlers;

public sealed class GetByIdQueryHandlerTests
{
    [Fact]
    public async Task GetTrackById_returns_track_when_found()
    {
        await using var ctx = new SqliteTestContext();
        var track = TestData.Track(title: "Found");
        ctx.Database.Tracks.Add(track);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = new GetTrackByIdQueryHandler(ctx.Database);
        var result = await sut.Handle(new GetTrackByIdQuery(track.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Found");
    }

    [Fact]
    public async Task GetTrackById_returns_not_found_when_missing()
    {
        await using var ctx = new SqliteTestContext();
        var sut = new GetTrackByIdQueryHandler(ctx.Database);

        var result = await sut.Handle(new GetTrackByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task GetPlayListById_returns_playlist_when_found()
    {
        await using var ctx = new SqliteTestContext();
        var playList = TestData.PlayList(Guid.NewGuid(), name: "MyList");
        ctx.Database.PlayLists.Add(playList);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = new GetPlayListByIdQueryHandler(ctx.Database);
        var result = await sut.Handle(new GetPlayListByIdQuery(playList.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("MyList");
    }

    [Fact]
    public async Task GetPlayListById_returns_not_found_when_missing()
    {
        await using var ctx = new SqliteTestContext();
        var sut = new GetPlayListByIdQueryHandler(ctx.Database);

        var result = await sut.Handle(new GetPlayListByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task GetUserById_returns_user_when_found()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User(name: "Carol");
        ctx.Database.Users.Add(user);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = new GetUserByIdQueryHandler(ctx.Database);
        var result = await sut.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Carol");
    }

    [Fact]
    public async Task GetUserById_returns_not_found_when_missing()
    {
        await using var ctx = new SqliteTestContext();
        var sut = new GetUserByIdQueryHandler(ctx.Database);

        var result = await sut.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }
}
