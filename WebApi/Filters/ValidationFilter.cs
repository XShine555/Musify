using FluentValidation;

namespace WebApi.Filters;

public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();

        if (argument is null)
            return await next(context);

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);

        return result.IsValid
            ? await next(context)
            : Results.ValidationProblem(result.ToDictionary());
    }
}