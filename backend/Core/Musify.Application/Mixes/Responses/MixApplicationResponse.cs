using Musify.Domain.Entities;

namespace Musify.Application.Mixes.Responses
{
    public record MixApplicationResponse(
        Guid Id,
        string Title,
        string? Subtitle,
        int ItemCount,
        IReadOnlyList<MixItemApplicationResponse> Items)
    {
        public const int CoverItemCount = 4;

        public static MixApplicationResponse FromEntity(
            Mix mix,
            int itemCount,
            IReadOnlyList<MixItem> items) =>
            new(
                mix.Id,
                mix.Title,
                mix.Subtitle,
                itemCount,
                [.. items.Select(MixItemApplicationResponse.FromEntity)]);
    }
}
