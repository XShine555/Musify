using Mediator;
using Musify.Application.Configuration;
using Musify.Application.Contracts;
using Musify.Application.Pictures;

namespace Musify.Api.Endpoints;

internal static class CoverEndpoint
{
    public static async Task<IResult> Stream(
        IMediator mediator,
        IStorageService storageService,
        HttpResponse response,
        CoverOwner owner,
        Guid id,
        string size,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCoverQuery(owner, id, PictureSizeParser.Parse(size)), cancellationToken);
        if (result.IsError)
            return Results.NotFound();

        var location = result.Value;
        var stream = await storageService.GetFileAsync(location.Bucket, location.Key, cancellationToken);
        if (stream == null)
            return Results.NotFound();

        response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return Results.Stream(stream, location.ContentType);
    }
}
