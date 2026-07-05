using Ardalis.Result;

namespace Musify.Application.Extensions;

public static class ResultExtensions
{
    public static Result<TOut> As<TIn, TOut>(this Result<TIn> result)
    {
        return result.Status switch
        {
            ResultStatus.Ok or
            ResultStatus.Created or
            ResultStatus.NoContent   => throw new InvalidOperationException($"Cannot convert a successful result ( {result.Status} ) to another type. Use Map or Bind instead."),
            ResultStatus.NotFound    => Result<TOut>.NotFound(result.Errors.ToArray()),
            ResultStatus.Forbidden   => Result<TOut>.Forbidden(result.Errors.ToArray()),
            ResultStatus.Unauthorized => Result<TOut>.Unauthorized(result.Errors.ToArray()),
            ResultStatus.Invalid     => Result<TOut>.Invalid(result.ValidationErrors.ToArray()),
            ResultStatus.Conflict    => Result<TOut>.Conflict(result.Errors.ToArray()),
            ResultStatus.CriticalError => Result<TOut>.CriticalError(result.Errors.ToArray()),
            ResultStatus.Unavailable => Result<TOut>.Unavailable(result.Errors.ToArray()),
            ResultStatus.Error       => Result<TOut>.Error(string.Join(";", result.Errors)),
            _                        => Result<TOut>.Error(string.Join(";", result.Errors))
        };
    }
}