using FluentAssertions;
using Musify.Application.Tests.Infrastructure;
using Musify.Application.Tracks.Handlers;
using Musify.Application.Tracks.Queries;
using Musify.Domain.Entities;
using Xunit;

namespace Musify.Application.Tests.Handlers;

public sealed class GetTracksByUserIdQueryHandlerTests
{
    private static async Task SeedTrackForUserAsync(SqliteTestContext ctx, Guid userId, string title, DateTime created)
    {
        var track = TestData.Track(title: title);
        track.CreatedDate = created;
        ctx.Database.Tracks.Add(track);
        ctx.Database.UserHasTracks.Add(new UserHasTrack { UserId = userId, TrackId = track.Id });
        await ctx.Database.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Returns_only_tracks_owned_by_the_user_ordered_by_created_date()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        await SeedTrackForUserAsync(ctx, userId, "Beta", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        await SeedTrackForUserAsync(ctx, userId, "Alpha", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await SeedTrackForUserAsync(ctx, otherUser, "Gamma", new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        var sut = new GetTracksByUserIdQueryHandler(ctx.Database);
        var result = await sut.Handle(new GetTracksByUserIdQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalItemCount.Should().Be(2);
        result.Value.Items.Select(t => t.Title).Should().Equal("Alpha", "Beta");
    }

    [Fact]
    public async Task Filters_by_normalized_name_contains()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        await SeedTrackForUserAsync(ctx, userId, "Summer Hit", DateTime.UtcNow);
        await SeedTrackForUserAsync(ctx, userId, "Winter Song", DateTime.UtcNow);

        var sut = new GetTracksByUserIdQueryHandler(ctx.Database);
        var result = await sut.Handle(new GetTracksByUserIdQuery(userId, Name: "summer"), CancellationToken.None);

        result.Value.TotalItemCount.Should().Be(1);
        result.Value.Items.Single().Title.Should().Be("Summer Hit");
    }

    [Fact]
    public async Task Normalizes_invalid_paging_arguments_to_defaults()
    {
        await using var ctx = new SqliteTestContext();
        var userId = Guid.NewGuid();
        for (var i = 0; i < 3; i++)
            await SeedTrackForUserAsync(ctx, userId, $"Track {i}", DateTime.UtcNow.AddSeconds(i));

        var sut = new GetTracksByUserIdQueryHandler(ctx.Database);
        var result = await sut.Handle(
            new GetTracksByUserIdQuery(userId, PageNumber: 0, PageSize: -5), CancellationToken.None);

        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalItemCount.Should().Be(3);
    }

    [Fact]
    public async Task Returns_empty_page_when_user_has_no_tracks()
    {
        await using var ctx = new SqliteTestContext();

        var sut = new GetTracksByUserIdQueryHandler(ctx.Database);
        var result = await sut.Handle(new GetTracksByUserIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalItemCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }
}
