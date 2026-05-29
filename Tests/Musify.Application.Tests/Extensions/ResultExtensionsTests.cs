using Ardalis.Result;
using FluentAssertions;
using Musify.Application.Extensions;
using Xunit;

namespace Musify.Application.Tests.Extensions;

public sealed class ResultExtensionsTests
{
    [Fact]
    public void NotFound_maps_to_NotFound_and_preserves_errors()
    {
        var source = Result<int>.NotFound("missing");

        var mapped = source.As<int, string>();

        mapped.Status.Should().Be(ResultStatus.NotFound);
        mapped.Errors.Should().Contain("missing");
    }

    [Fact]
    public void Conflict_maps_to_Conflict()
    {
        Result<int>.Conflict("dup").As<int, string>().Status.Should().Be(ResultStatus.Conflict);
    }

    [Fact]
    public void Unauthorized_maps_to_Unauthorized()
    {
        Result<int>.Unauthorized().As<int, string>().Status.Should().Be(ResultStatus.Unauthorized);
    }

    [Fact]
    public void Forbidden_maps_to_Forbidden()
    {
        Result<int>.Forbidden().As<int, string>().Status.Should().Be(ResultStatus.Forbidden);
    }

    [Fact]
    public void Invalid_maps_to_Invalid_and_preserves_validation_errors()
    {
        var source = Result<int>.Invalid(new ValidationError("field", "bad"));

        var mapped = source.As<int, string>();

        mapped.Status.Should().Be(ResultStatus.Invalid);
        mapped.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage == "bad");
    }

    [Fact]
    public void Error_maps_to_Error()
    {
        Result<int>.Error("boom").As<int, string>().Status.Should().Be(ResultStatus.Error);
    }

    [Theory]
    [InlineData(ResultStatus.Ok)]
    [InlineData(ResultStatus.Created)]
    [InlineData(ResultStatus.NoContent)]
    public void Successful_results_throw_when_converted(ResultStatus status)
    {
        Result<int> source = status switch
        {
            ResultStatus.Ok => Result<int>.Success(1),
            ResultStatus.Created => Result<int>.Created(1),
            ResultStatus.NoContent => Result<int>.NoContent(),
            _ => throw new InvalidOperationException()
        };

        var act = () => source.As<int, string>();

        act.Should().Throw<InvalidOperationException>();
    }
}
