namespace Musify.Application.Shared
{
    public static class TextNormalizer
    {
        public static string Normalize(string value) => value.Trim().ToUpperInvariant();
    }
}
