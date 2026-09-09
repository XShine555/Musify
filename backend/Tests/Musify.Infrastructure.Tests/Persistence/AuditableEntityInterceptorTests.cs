using Microsoft.EntityFrameworkCore;
using Musify.Domain.Entities;
using Musify.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Musify.Infrastructure.Tests.Persistence;

[Collection(InfrastructureCollection.Name)]
public sealed class AuditableEntityInterceptorTests(InfrastructureTestFixture fixture)
{
    [Fact]
    public async Task SavingChanges_NewEntity_SetsCreatedAtAndUpdatedAtToNow()
    {
        await using var database = fixture.CreateDatabase();
        var before = DateTime.UtcNow;

        var user = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Name = "audit-user", NormalizedName = "AUDIT-USER" };
        await database.Users.AddAsync(user);
        await database.SaveChangesAsync(CancellationToken.None);

        var after = DateTime.UtcNow;

        Assert.InRange(user.CreatedAt, before, after);
        Assert.InRange(user.UpdatedAt, before, after);
    }

    [Fact]
    public async Task SavingChanges_ModifiedEntity_BumpsUpdatedAtButKeepsCreatedAt()
    {
        await using var database = fixture.CreateDatabase();

        var user = new User { Id = Random.Shared.NextInt64(1, long.MaxValue), Name = "audit-user-2", NormalizedName = "AUDIT-USER-2" };
        await database.Users.AddAsync(user);
        await database.SaveChangesAsync(CancellationToken.None);
        var originalCreatedAt = user.CreatedAt;

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        user.Name = "audit-user-2-renamed";
        await database.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(originalCreatedAt, user.CreatedAt);
        Assert.True(user.UpdatedAt > originalCreatedAt,
            $"original-created-at={originalCreatedAt:O}, final-updated-at={user.UpdatedAt:O}");
    }
}
