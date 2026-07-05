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

    private static IResult Problem(List<Error> errors)
    {
        if (errors.All(e => e.Type == ErrorType.Validation))
            return Results.ValidationProblem(ToValidationDictionary(errors));

        var error = errors[0];
        return error.Type switch
        {
            ErrorType.NotFound => Results.NotFound(error.Description),
            ErrorType.Unauthorized => Results.Unauthorized(),
            ErrorType.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
            ErrorType.Conflict => Results.Conflict(error.Description),
            _ => Results.Problem(error.Description)
        };
    }

    private static Dictionary<string, string[]> ToValidationDictionary(IEnumerable<Error> errors) =>
        errors
            .GroupBy(e => string.IsNullOrEmpty(e.Code) ? "general" : e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
}
