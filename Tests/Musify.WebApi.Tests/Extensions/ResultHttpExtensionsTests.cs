using Ardalis.Result;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using WebApi.Extensions;
using Xunit;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Musify.WebApi.Tests.Extensions;

public sealed class ResultHttpExtensionsTests
{
    private static int StatusCodeOf(IResult result) =>
        ((IStatusCodeHttpResult)result).StatusCode ?? 0;

    [Fact]
    public void Result_Ok_maps_to_204_no_content()
    {
        StatusCodeOf(Result.Success().ToHttpResult()).Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public void Result_NoContent_maps_to_204()
    {
        StatusCodeOf(Result.NoContent().ToHttpResult()).Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public void Result_NotFound_maps_to_404()
    {
        StatusCodeOf(Result.NotFound("nope").ToHttpResult()).Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Result_Invalid_maps_to_validation_problem_400()
    {
        var result = Result.Invalid(new ValidationError("field", "bad")).ToHttpResult();
        result.Should().BeOfType<ProblemHttpResult>();
        StatusCodeOf(result).Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Result_Unauthorized_maps_to_401()
    {
        StatusCodeOf(Result.Unauthorized().ToHttpResult()).Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void Result_Forbidden_maps_to_403()
    {
        StatusCodeOf(Result.Forbidden().ToHttpResult()).Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void Result_Conflict_maps_to_409()
    {
        StatusCodeOf(Result.Conflict("dup").ToHttpResult()).Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void Result_Error_maps_to_problem_500()
    {
        var result = Result.Error("boom").ToHttpResult();
        result.Should().BeOfType<ProblemHttpResult>();
        StatusCodeOf(result).Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public void ResultT_Ok_maps_to_200_with_value()
    {
        var result = Result<string>.Success("payload").ToHttpResult();
        result.Should().BeOfType<Ok<string>>();
        result.As<Ok<string>>().Value.Should().Be("payload");
    }

    [Fact]
    public void ResultT_NotFound_maps_to_404()
    {
        StatusCodeOf(Result<string>.NotFound("nope").ToHttpResult()).Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ResultT_Invalid_maps_to_validation_problem_with_grouped_errors()
    {
        var result = Result<string>.Invalid(
            new ValidationError("Name", "required"),
            new ValidationError("Name", "too short"),
            new ValidationError("Age", "invalid")).ToHttpResult();

        result.Should().BeOfType<ProblemHttpResult>();
        var details = result.As<ProblemHttpResult>().ProblemDetails.Should().BeOfType<HttpValidationProblemDetails>().Subject;
        details.Errors.Should().ContainKey("Name");
        details.Errors["Name"].Should().BeEquivalentTo("required", "too short");
        details.Errors.Should().ContainKey("Age");
    }

    [Fact]
    public void ResultT_Unauthorized_maps_to_401()
    {
        StatusCodeOf(Result<string>.Unauthorized().ToHttpResult()).Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void ResultT_Forbidden_maps_to_403()
    {
        StatusCodeOf(Result<string>.Forbidden().ToHttpResult()).Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void ResultT_Conflict_maps_to_409()
    {
        StatusCodeOf(Result<string>.Conflict("dup").ToHttpResult()).Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void ResultT_Error_maps_to_problem_500()
    {
        var result = Result<string>.Error("boom").ToHttpResult();
        result.Should().BeOfType<ProblemHttpResult>();
        StatusCodeOf(result).Should().Be(StatusCodes.Status500InternalServerError);
    }
}
