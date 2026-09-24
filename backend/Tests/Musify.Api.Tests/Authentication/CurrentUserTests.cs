using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Musify.Api.Authentication;
using Xunit;

namespace Musify.Api.Tests.Authentication;

public sealed class CurrentUserTests
{
    private static async Task<CurrentUser> BindAsync(ClaimsPrincipal principal)
    {
        var context = new DefaultHttpContext { User = principal };
        var bound = await CurrentUser.BindAsync(context, parameter: null!);
        return bound!;
    }

    private static ClaimsPrincipal AuthenticatedPrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "TestAuth"));

    [Fact]
    public async Task BindAsync_AuthenticatedPrincipalWithValidClaims_PopulatesEveryField()
    {
        var principal = AuthenticatedPrincipal(
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Name, "jdoe"),
            new Claim(ClaimTypes.GivenName, "Jane"),
            new Claim(ClaimTypes.Surname, "Doe"));

        var user = await BindAsync(principal);

        Assert.True(user.IsAuthenticated);
        Assert.Equal(42, user.Id);
        Assert.Equal(42, user.RequiredId);
        Assert.Equal("jdoe", user.Username);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
    }

    [Fact]
    public async Task BindAsync_AnonymousPrincipal_IsNotAuthenticatedAndHasNoId()
    {
        var user = await BindAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(user.IsAuthenticated);
        Assert.Null(user.Id);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("0")]
    [InlineData("")]
    public async Task BindAsync_NonNumericOrZeroNameIdentifier_LeavesIdNull(string rawId)
    {
        var user = await BindAsync(AuthenticatedPrincipal(new Claim(ClaimTypes.NameIdentifier, rawId)));

        Assert.Null(user.Id);
    }

    [Fact]
    public async Task RequiredId_MissingId_Throws()
    {
        var user = await BindAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.Throws<InvalidOperationException>(() => user.RequiredId);
    }
}
