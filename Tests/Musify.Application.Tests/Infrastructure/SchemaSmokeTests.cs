using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Musify.Application.Tests.Infrastructure;

public sealed class SchemaSmokeTests
{
    [Fact]
    public async Task Schema_creates_and_persists_all_core_entities()
    {
        await using var ctx = new SqliteTestContext();

        var user = TestData.User();
        ctx.Database.Users.Add(user);
        ctx.Database.UploadIntents.Add(TestData.UploadIntent(user.Id));
        ctx.Database.PlayLists.Add(TestData.PlayList(user.Id));
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var verify = ctx.NewContext();
        (await verify.Users.CountAsync()).Should().Be(1);
        (await verify.UploadIntents.CountAsync()).Should().Be(1);
        (await verify.PlayLists.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Transactions_are_supported_under_sqlite()
    {
        await using var ctx = new SqliteTestContext();
        var user = TestData.User();

        await using (var tx = await ctx.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, CancellationToken.None))
        {
            ctx.Database.Users.Add(user);
            await ctx.Database.SaveChangesAsync(CancellationToken.None);
            await tx.CommitAsync(CancellationToken.None);
        }

        (await ctx.NewContext().Users.CountAsync()).Should().Be(1);
    }
}
