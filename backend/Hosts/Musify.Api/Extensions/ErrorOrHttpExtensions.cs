using ErrorOr;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Musify.Api.Extensions;

public static class ErrorOrHttpExtensions
{
    public static IResult ToHttpResult<T>(this ErrorOr<T> result)
    {
        if (result.IsError)
            return Problem(result.Errors);

        return result.Value is Success
            ? Results.NoContent()
            : Results.Ok(result.Value);
    }

    public static IResult ToCreatedResult<T>(this ErrorOr<T> result, Func<T, string> locationFactory) =>
        result.IsError
            ? Problem(result.Errors)
            : Results.Created(locationFactory(result.Value), result.Value);

    private static IResult Problem(List<Error> errors)
    {
        if (errors.All(e => e.Type == ErrorType.Validation))
            return Results.ValidationProblem(ToValidationDictionary(errors));

        var error = errors[0];
        return error.Type switch
        {
            ErrorType.NotFound => Results.Problem(error.Description, statusCode: StatusCodes.Status404NotFound),
            ErrorType.Unauthorized => Results.Unauthorized(),
            ErrorType.Forbidden => Results.Problem(error.Description, statusCode: StatusCodes.Status403Forbidden),
            ErrorType.Conflict => Results.Problem(error.Description, statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(error.Description)
        };
    }

    private static Dictionary<string, string[]> ToValidationDictionary(IEnumerable<Error> errors) =>
        errors
            .GroupBy(e => string.IsNullOrEmpty(e.Code) ? "general" : e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
}
