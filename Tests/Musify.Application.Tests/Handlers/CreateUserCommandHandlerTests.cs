using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Musify.Application.Tests.Infrastructure;
using Musify.Application.Users.Commands;
using Musify.Application.Users.Handlers;
using Xunit;

namespace Musify.Application.Tests.Handlers;

public sealed class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task When_user_already_exists_returns_conflict()
    {
        await using var ctx = new SqliteTestContext();
        var existing = TestData.User();
        ctx.Database.Users.Add(existing);
        await ctx.Database.SaveChangesAsync(CancellationToken.None);

        var sut = new CreateUserCommandHandler(ctx.Database, NullLogger<CreateUserCommandHandler>.Instance);
        var result = await sut.Handle(new CreateUserCommand(existing.Id, "Bob", null, null), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Conflict);
    }

    [Fact]
    public async Task Happy_path_creates_user_and_persists()
    {
        await using var ctx = new SqliteTestContext();
        var id = Guid.NewGuid();

        var sut = new CreateUserCommandHandler(ctx.Database, NullLogger<CreateUserCommandHandler>.Instance);
        var result = await sut.Handle(new CreateUserCommand(id, "Bob", "Robert", "Smith"), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Created);
        result.Value.Name.Should().Be("Bob");

        var persisted = await ctx.NewContext().Users.SingleAsync(u => u.Id == id);
        persisted.NormalizedName.Should().Be("BOB");
        persisted.FirstName.Should().Be("Robert");
    }
}
