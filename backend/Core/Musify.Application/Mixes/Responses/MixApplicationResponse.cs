using Musify.Domain.ValueObjects;

namespace Musify.Application.Mixes.Responses;

public record MixApplicationResponse(
    Guid Id,
    MixKind Kind,
    int ItemCount,
    IReadOnlyList<MixItemApplicationResponse> Items)
{
    public const int CoverItemCount = 4;
}
