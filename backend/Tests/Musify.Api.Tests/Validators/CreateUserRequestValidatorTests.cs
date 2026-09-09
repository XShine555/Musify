using Musify.Api.DataTransferObjects.Users;
using Musify.Api.Validators.Users;
using Xunit;

namespace Musify.Api.Tests.Validators;

public sealed class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var result = validator.Validate(new CreateUserRequest(1, "Jane Doe", "Jane", "Doe"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ZeroId_Fails()
    {
        var result = validator.Validate(new CreateUserRequest(0, "Jane Doe", null, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserRequest.Id));
    }

    [Fact]
    public void Validate_BlankName_Fails()
    {
        var result = validator.Validate(new CreateUserRequest(1, "", null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NameOverMaxLength_Fails()
    {
        var result = validator.Validate(new CreateUserRequest(1, new string('a', 49), null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_OptionalNamesOmitted_Passes()
    {
        var result = validator.Validate(new CreateUserRequest(1, "Jane Doe", null, null));

        Assert.True(result.IsValid);
    }
}
