using FluentAssertions;
using Musify.Application.PlayLists.Handlers;
using Musify.Application.PlayLists.Queries;
using Musify.Application.Tests.Infrastructure;
using Musify.Application.Tracks.Handlers;
using Musify.Application.Tracks.Queries;
using Musify.Application.Users.Handlers;
using Musify.Application.Users.Queries;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Handlers;

public sealed class ListQueryHandlerTests
{
    [Fact]
    public async Task GetTracks_paginates_across_all_users()
    {
        await using var ctx = new SqliteTestContext();
        for (var i = 0; i < 15; i++)
        {
            var track = TestData.Track(title: $"Track {i:D2}");
            ctx.Database.Tracks.Add(track);
            ctx.Database.UserHasTracks.Add(new UserHasTrack { UserId = Guid.NewGuid(), TrackId = track.Id });
        }
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = new GetTracksQueryHandler(ctx.Database);
        var result = await sut.Handle(new GetTracksQuery(PageNumber: 2, PageSize: 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalItemCount.Should().Be(15);
        result.Value.PageNumber.Should().Be(2);
        result.Value.Items.Should().HaveCount(5);
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetTracks_normalizes_invalid_paging_to_defaults()
    {
        await using var ctx = new SqliteTestContext();
        var track = TestData.Track();
        ctx.Database.Tracks.Add(track);
        ctx.Database.UserHasTracks.Add(new UserHasTrack { UserId = Guid.NewGuid(), TrackId = track.Id });
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = new GetTracksQueryHandler(ctx.Database);
        var result = await sut.Handle(new GetTracksQuery(PageNumber: 0, PageSize: -3), CancellationToken.None);

        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalItemCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPlayLists_returns_all_playlists_paginated()
    {
        await using var ctx = new SqliteTestContext();
        for (var i = 0; i < 3; i++)
            ctx.Database.PlayLists.Add(TestData.PlayList(Guid.NewGuid(), name: $"PL {i}"));
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = new GetPlayListsQueryHandler(ctx.Database);
        var result = await sut.Handle(new GetPlayListsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalItemCount.Should().Be(3);
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPlayListsByUserId_filters_by_user_and_name()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        ctx.Database.PlayLists.Add(TestData.PlayList(userId, name: "Summer Mix"));
        ctx.Database.PlayLists.Add(TestData.PlayList(userId, name: "Winter Mix"));
        ctx.Database.PlayLists.Add(TestData.PlayList(Guid.NewGuid(), name: "Summer Other"));
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = new GetPlayListsByUserIdQueryHandler(ctx.Database);

        var all = await sut.Handle(new GetPlayListsByUserIdQuery(userId), CancellationToken.None);
        all.Value.TotalItemCount.Should().Be(2);

        var filtered = await sut.Handle(new GetPlayListsByUserIdQuery(userId, Name: "summer"), CancellationToken.None);
        filtered.Value.TotalItemCount.Should().Be(1);
        filtered.Value.Items.Single().Name.Should().Be("Summer Mix");
    }

    [Fact]
    public async Task GetPlayListsByUserId_returns_empty_for_unknown_user()
    {
        await using var ctx = new SqliteTestContext();
        var sut = new GetPlayListsByUserIdQueryHandler(ctx.Database);

        var result = await sut.Handle(new GetPlayListsByUserIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalItemCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUsers_filters_by_username_search_case_insensitively()
    {
        await using var ctx = new SqliteTestContext();
        ctx.Database.Users.Add(TestData.User(name: "Alice"));
        ctx.Database.Users.Add(TestData.User(name: "Bob"));
        ctx.Database.Users.Add(TestData.User(name: "Alicia"));
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = new GetUsersQueryHandler(ctx.Database);
        var result = await sut.Handle(new GetUsersQuery(1, 10, UsernameSearch: "ali"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalItemCount.Should().Be(2);
        result.Value.Items.Select(u => u.Name).Should().BeEquivalentTo("Alice", "Alicia");
    }

    [Fact]
    public async Task GetUsers_normalizes_invalid_paging_to_defaults()
    {
        await using var ctx = new SqliteTestContext();
        ctx.Database.Users.Add(TestData.User(name: "Solo"));
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = new GetUsersQueryHandler(ctx.Database);
        var result = await sut.Handle(new GetUsersQuery(0, -1), CancellationToken.None);

        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalItemCount.Should().Be(1);
    }
}
