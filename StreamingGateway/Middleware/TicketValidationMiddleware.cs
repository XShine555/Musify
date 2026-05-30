using Microsoft.Extensions.Options;
using Musify.StreamingGateway.Authentication;
using Musify.StreamingGateway.Configuration;

namespace Musify.StreamingGateway.Middleware;

public sealed class TicketValidationMiddleware(
    RequestDelegate next,
    TicketValidator ticketValidator,
    IOptions<StreamTicketValidationOptions> options,
    ILogger<TicketValidationMiddleware> logger)
{
    private readonly StreamTicketValidationOptions options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        if (!path.StartsWithSegments(options.MediaPathPrefix, out var remaining))
        {
            await next(context);
            return;
        }

        var token = context.Request.Query[options.QueryParameterName].ToString();
        var validation = await ticketValidator.ValidateTicketAsync(token);

        if (!validation.IsSuccess)
        {
            logger.LogWarning("Rejected media request {Path}: missing or invalid ticket", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var prefix = validation.Value;

        var objectKey = remaining.Value?.TrimStart('/') ?? string.Empty;
        if (!objectKey.StartsWith(prefix, StringComparison.Ordinal))
        {
            logger.LogWarning("Rejected media request {ObjectKey}: outside authorized prefix {Prefix}", objectKey, prefix);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        StripQueryParameter(context, options.QueryParameterName);

        await next(context);
    }

    private static void StripQueryParameter(HttpContext context, string name)
    {
        var remaining = context.Request.Query
            .Where(pair => !string.Equals(pair.Key, name, StringComparison.Ordinal))
            .SelectMany(pair => pair.Value.Select(value => new KeyValuePair<string, string?>(pair.Key, value)))
            .ToList();

        context.Request.QueryString = QueryString.Create(remaining);
    }
}
