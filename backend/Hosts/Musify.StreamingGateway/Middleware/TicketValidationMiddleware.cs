using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Musify.StreamingGateway.Authentication;
using Musify.StreamingGateway.Configuration;

namespace Musify.StreamingGateway.Middleware;

public sealed class TicketValidationMiddleware(
    RequestDelegate next,
    TicketValidator ticketValidator,
    StreamTicketValidationConfiguration options,
    ILogger<TicketValidationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        if (!path.StartsWithSegments(options.MediaPathPrefix, out var remaining))
        {
            await next(context);
            return;
        }

        var token = context.Request.Query[options.QueryParameterName].ToString();
        if (string.IsNullOrEmpty(token))
        {
            token = context.Request.Headers[options.HeaderName].ToString();
        }

        var validation = await ticketValidator.ValidateTicketAsync(token);

        if (!validation.IsValid)
        {
            logger.LogWarning("Rejected media request {Path}: missing or invalid ticket", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var prefix = validation.Prefix;
        var objectKey = remaining.Value?.TrimStart('/') ?? string.Empty;

        if (HasTraversalSegment(objectKey))
        {
            logger.LogWarning("Rejected media request {ObjectKey}: contains a path traversal segment", objectKey);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var prefixBoundary = prefix.EndsWith('/') ? prefix : prefix + "/";
        if (!string.Equals(objectKey, prefix, StringComparison.Ordinal)
            && !objectKey.StartsWith(prefixBoundary, StringComparison.Ordinal))
        {
            logger.LogWarning("Rejected media request {ObjectKey}: outside authorized prefix {Prefix}", objectKey, prefix);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (validation.MaxBytes is { } maxBytes && !TryClampRange(context, maxBytes))
        {
            logger.LogInformation(
                "Rejected media request {ObjectKey}: requested range is a suffix range or starts beyond the {MaxBytes}-byte preview limit",
                objectKey, maxBytes);
            context.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
            context.Response.Headers[HeaderNames.ContentRange] = $"bytes */{maxBytes}";
            return;
        }

        StripQueryParameter(context, options.QueryParameterName);
        await next(context);
    }

    private static bool TryClampRange(HttpContext context, long maxBytes)
    {
        var rangeHeader = context.Request.Headers.Range;
        if (StringValues.IsNullOrEmpty(rangeHeader))
        {
            context.Request.Headers.Range = $"bytes=0-{maxBytes - 1}";
            return true;
        }

        if (!RangeHeaderValue.TryParse(rangeHeader.ToString(), out var range)
            || range.Ranges.Count != 1
            || !string.Equals(range.Unit.ToString(), "bytes", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Headers.Range = $"bytes=0-{maxBytes - 1}";
            return true;
        }

        var requested = range.Ranges.Single();
        if (requested.From is null)
            return false;

        var start = requested.From.Value;
        if (start >= maxBytes)
            return false;

        var end = requested.To is { } requestedEnd ? Math.Min(requestedEnd, maxBytes - 1) : maxBytes - 1;
        context.Request.Headers.Range = $"bytes={start}-{end}";
        return true;
    }

    private static bool HasTraversalSegment(string objectKey) =>
        objectKey.Split('/').Any(segment => segment is ".." or ".");

    private static void StripQueryParameter(HttpContext context, string name)
    {
        var remaining = context.Request.Query
            .Where(pair => !string.Equals(pair.Key, name, StringComparison.Ordinal))
            .SelectMany(pair => pair.Value.Select(value => new KeyValuePair<string, string?>(pair.Key, value)))
            .ToList();

        context.Request.QueryString = QueryString.Create(remaining);
    }
}
