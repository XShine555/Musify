using Ardalis.Result;

namespace Musify.Application.Extensions
{
    public static class ResultExtensions
    {
        public static Result ToErrorResult<T>(this Result<T> result)
        {
            var newResult = result.Status switch
            {
                ResultStatus.Error => Result.Error(new ErrorList(result.Errors)),
                ResultStatus.Forbidden => Result.Forbidden(result.Errors.ToArray()),
                ResultStatus.Unauthorized => Result.Unauthorized(result.Errors.ToArray()),
                ResultStatus.Invalid => Result.Invalid(result.ValidationErrors.ToArray()),
                ResultStatus.NotFound => Result.NotFound(result.Errors.ToArray()),
                ResultStatus.Conflict => Result.Conflict(result.Errors.ToArray()),
                ResultStatus.CriticalError => Result.CriticalError(result.Errors.ToArray()),
                ResultStatus.Unavailable => Result.Unavailable(result.Errors.ToArray()),
                _ => throw new NotSupportedException($"Result {result.Status} conversion is not supported.")
            };
            return newResult;
        }
    }
}