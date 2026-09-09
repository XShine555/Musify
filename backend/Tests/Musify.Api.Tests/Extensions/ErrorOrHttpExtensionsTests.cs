using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Musify.Api.Extensions;
using Xunit;

namespace Musify.Api.Tests.Extensions
{
    public sealed class ErrorOrHttpExtensionsTests
    {
        // The concrete IResult types (NoContent, UnauthorizedHttpResult, ...) resolve logging/problem-
        // details services from HttpContext.RequestServices when they execute, so it can't be empty.
        private static readonly IServiceProvider Services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();

        private static async Task<(int StatusCode, string Body)> ExecuteAsync(IResult result)
        {
            var context = new DefaultHttpContext { RequestServices = Services };
            var bodyStream = new MemoryStream();
            context.Response.Body = bodyStream;

            await result.ExecuteAsync(context);

            bodyStream.Position = 0;
            using var reader = new StreamReader(bodyStream);
            return (context.Response.StatusCode, await reader.ReadToEndAsync());
        }

        [Fact]
        public async Task ToHttpResult_Success_ReturnsNoContent()
        {
            ErrorOr<Success> result = new Success();

            var (statusCode, _) = await ExecuteAsync(result.ToHttpResult());

            Assert.Equal(StatusCodes.Status204NoContent, statusCode);
        }

        [Fact]
        public async Task ToHttpResult_Value_ReturnsOkWithTheValue()
        {
            ErrorOr<string> result = "hello";

            var (statusCode, body) = await ExecuteAsync(result.ToHttpResult());

            Assert.Equal(StatusCodes.Status200OK, statusCode);
            Assert.Contains("hello", body);
        }

        [Fact]
        public async Task ToHttpResult_NotFoundError_ReturnsNotFound()
        {
            ErrorOr<string> result = Error.NotFound(description: "not here");

            var (statusCode, _) = await ExecuteAsync(result.ToHttpResult());

            Assert.Equal(StatusCodes.Status404NotFound, statusCode);
        }

        [Fact]
        public async Task ToHttpResult_UnauthorizedError_ReturnsUnauthorized()
        {
            ErrorOr<string> result = Error.Unauthorized();

            var (statusCode, _) = await ExecuteAsync(result.ToHttpResult());

            Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
        }

        [Fact]
        public async Task ToHttpResult_ForbiddenError_ReturnsForbidden()
        {
            ErrorOr<string> result = Error.Forbidden();

            var (statusCode, _) = await ExecuteAsync(result.ToHttpResult());

            Assert.Equal(StatusCodes.Status403Forbidden, statusCode);
        }

        [Fact]
        public async Task ToHttpResult_ConflictError_ReturnsConflict()
        {
            ErrorOr<string> result = Error.Conflict();

            var (statusCode, _) = await ExecuteAsync(result.ToHttpResult());

            Assert.Equal(StatusCodes.Status409Conflict, statusCode);
        }

        [Fact]
        public async Task ToHttpResult_FailureError_ReturnsProblem500()
        {
            ErrorOr<string> result = Error.Failure();

            var (statusCode, _) = await ExecuteAsync(result.ToHttpResult());

            Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        }

        [Fact]
        public async Task ToHttpResult_ValidationErrors_ReturnsValidationProblem()
        {
            ErrorOr<string> result = new List<Error>
            {
                Error.Validation("Name", "Name is required"),
                Error.Validation("Name", "Name is too long"),
                Error.Validation("Age", "Age must be positive"),
            };

            var (statusCode, body) = await ExecuteAsync(result.ToHttpResult());

            Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
            Assert.Contains("Name is required", body);
            Assert.Contains("Age must be positive", body);
        }

        [Fact]
        public async Task ToCreatedResult_Success_ReturnsCreatedAtTheGivenLocation()
        {
            ErrorOr<string> result = "the-id";

            var context = new DefaultHttpContext { RequestServices = Services, Response = { Body = new MemoryStream() } };
            await result.ToCreatedResult(value => $"/things/{value}").ExecuteAsync(context);

            Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
            Assert.Equal("/things/the-id", context.Response.Headers.Location);
        }

        [Fact]
        public async Task ToCreatedResult_Error_ReturnsTheMappedErrorStatus()
        {
            ErrorOr<string> result = Error.Conflict();

            var (statusCode, _) = await ExecuteAsync(result.ToCreatedResult(value => $"/things/{value}"));

            Assert.Equal(StatusCodes.Status409Conflict, statusCode);
        }
    }
}
