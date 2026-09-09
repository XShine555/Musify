namespace Musify.Application.Shared
{
    public static class StorageKey
    {
        public static string Combine(params string[] segments)
        {
            return string.Join('/', segments
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .Select(static s => s.Trim().Trim('/', '\\')));
        }
    }
}
