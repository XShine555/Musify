using Musify.Api.DataTransferObjects.Albums;
using Musify.Api.Validators.Albums;
using Xunit;

namespace Musify.Api.Tests.Validators;

public sealed class CreateAlbumRequestValidatorTests
{
    private readonly CreateAlbumRequestValidator validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var result = validator.Validate(new CreateAlbumRequest("My Album", "A short description", 2024));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidRequestWithoutOptionalFields_Passes()
    {
        var result = validator.Validate(new CreateAlbumRequest("My Album", null, null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_BlankTitle_Fails()
    {
        var result = validator.Validate(new CreateAlbumRequest("", null, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAlbumRequest.Title));
    }

    [Fact]
    public void Validate_TitleOverMaxLength_Fails()
    {
        var result = validator.Validate(new CreateAlbumRequest(new string('a', 201), null, null));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(1876)]
    [InlineData(1877)]
    public void Validate_ReleaseYearAtOrBeforeTheEarliestBoundary(int year)
    {
        var result = validator.Validate(new CreateAlbumRequest("Title", null, year));

        Assert.Equal(year >= 1877, result.IsValid);
    }

    [Fact]
    public void Validate_ReleaseYearMoreThanOneYearInTheFuture_Fails()
    {
        var result = validator.Validate(new CreateAlbumRequest("Title", null, DateTime.UtcNow.Year + 2));

        Assert.False(result.IsValid);
    }
}
