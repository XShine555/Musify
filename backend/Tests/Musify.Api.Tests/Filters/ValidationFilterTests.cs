using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Musify.Api.Filters;
using NSubstitute;
using Xunit;

namespace Musify.Api.Tests.Filters;

public sealed class ValidationFilterTests
{
    public sealed record Payload(string Name);

    private static EndpointFilterInvocationContext CreateContext<T>(T argument) =>
        EndpointFilterInvocationContext.Create(new DefaultHttpContext(), argument);

    [Fact]
    public async Task InvokeAsync_ValidPayload_CallsNext()
    {
        var validator = Substitute.For<IValidator<Payload>>();
        validator.ValidateAsync(Arg.Any<Payload>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        var filter = new ValidationFilter<Payload>(validator);
        var context = CreateContext(new Payload("ok"));

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("next-called"));

        Assert.Equal("next-called", result);
        await validator.Received(1).ValidateAsync(Arg.Is<Payload>(p => p.Name == "ok"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_InvalidPayload_ReturnsValidationProblemWithoutCallingNext()
    {
        var validator = Substitute.For<IValidator<Payload>>();
        validator.ValidateAsync(Arg.Any<Payload>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "Name is required")]));
        var filter = new ValidationFilter<Payload>(validator);
        var context = CreateContext(new Payload(""));
        var nextCalled = false;

        var result = await filter.InvokeAsync(context, _ => { nextCalled = true; return ValueTask.FromResult<object?>(null); });

        Assert.False(nextCalled);
        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_NoMatchingArgument_CallsNextWithoutValidating()
    {
        var validator = Substitute.For<IValidator<Payload>>();
        var filter = new ValidationFilter<Payload>(validator);
        var context = CreateContext(42);

        var result = await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("next-called"));

        Assert.Equal("next-called", result);
        await validator.DidNotReceiveWithAnyArgs().ValidateAsync(Arg.Any<Payload>(), Arg.Any<CancellationToken>());
    }
}
