using Ardalis.Result;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Musify.Api.Extensions;

public static class ResultHttpExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.Status switch
        {
            ResultStatus.Ok or ResultStatus.NoContent => Results.NoContent(),
            ResultStatus.NotFound => Results.NotFound(result.Errors.FirstOrDefault()),
            ResultStatus.Invalid => Results.ValidationProblem(ToValidationDictionary(result.ValidationErrors)),
            ResultStatus.Unauthorized => Results.Unauthorized(),
            ResultStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
            ResultStatus.Conflict => Results.Conflict(result.Errors.FirstOrDefault()),
            _ => Results.Problem(string.Join(", ", result.Errors))
        };

    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.Status switch
        {
            ResultStatus.Ok => Results.Ok(result.Value),
            ResultStatus.NotFound => Results.NotFound(result.Errors.FirstOrDefault()),
            ResultStatus.Invalid => Results.ValidationProblem(ToValidationDictionary(result.ValidationErrors)),
            ResultStatus.Unauthorized => Results.Unauthorized(),
            ResultStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
            ResultStatus.Conflict => Results.Conflict(result.Errors.FirstOrDefault()),
            _ => Results.Problem(string.Join(", ", result.Errors))
        };

    private static Dictionary<string, string[]> ToValidationDictionary(IEnumerable<ValidationError> errors) =>
        errors
            .GroupBy(e => string.IsNullOrEmpty(e.Identifier) ? "general" : e.Identifier)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
