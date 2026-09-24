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
        var result = validator.Validate(new CreateAlbumRequest("My Album", "A short description", 2024, Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidRequestWithoutOptionalFields_Passes()
    {
        var result = validator.Validate(new CreateAlbumRequest("My Album", null, null, Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_BlankTitle_Fails()
    {
        var result = validator.Validate(new CreateAlbumRequest("", null, null, Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAlbumRequest.Title));
    }

    [Fact]
    public void Validate_TitleOverMaxLength_Fails()
    {
        var result = validator.Validate(new CreateAlbumRequest(new string('a', 201), null, null, Guid.NewGuid()));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ReleaseYearBeforeTheEarliestBoundary_Fails()
    {
        var result = validator.Validate(new CreateAlbumRequest("Title", null, AlbumReleaseYear.Earliest - 1, Guid.NewGuid()));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ReleaseYearAtTheEarliestBoundary_Passes()
    {
        var result = validator.Validate(new CreateAlbumRequest("Title", null, AlbumReleaseYear.Earliest, Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ReleaseYearAtTheLatestBoundary_Passes()
    {
        var result = validator.Validate(new CreateAlbumRequest("Title", null, AlbumReleaseYear.Latest, Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ReleaseYearPastTheLatestBoundary_Fails()
    {
        var result = validator.Validate(new CreateAlbumRequest("Title", null, AlbumReleaseYear.Latest + 1, Guid.NewGuid()));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyPictureIntentId_Fails()
    {
        var result = validator.Validate(new CreateAlbumRequest("Title", null, null, Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAlbumRequest.PictureIntentId));
    }
}
