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
    }
}
