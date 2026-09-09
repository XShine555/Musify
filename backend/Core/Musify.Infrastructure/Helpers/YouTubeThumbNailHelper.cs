namespace Musify.Infrastructure.Helpers
{
    public static class YouTubeThumbNailHelper
    {
        public static bool IsSquare(string url) =>
            url.Contains("googleusercontent.com", StringComparison.OrdinalIgnoreCase);

        public static string WithSize(string url, int size)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            if (url.Contains("googleusercontent.com", StringComparison.OrdinalIgnoreCase))
                return ResizeGoogleUserContent(url, size);

            if (url.Contains("ytimg.com", StringComparison.OrdinalIgnoreCase))
                return ResizeYtImg(url, size);

            return url;
        }

        private static string ResizeGoogleUserContent(string url, int size)
        {
            var separator = url.LastIndexOf('=');
            var baseUrl = separator > 0 && IsSizeSuffix(url.AsSpan(separator + 1))
                ? url[..separator]
                : url;

            return $"{baseUrl}=w{size}-h{size}-l90-rj";
        }

        private static string ResizeYtImg(string url, int size)
        {
            var queryStart = url.IndexOf('?');
            var path = queryStart >= 0 ? url[..queryStart] : url;

            var lastSlash = path.LastIndexOf('/');
            if (lastSlash < 0)
                return path;

            return $"{path[..lastSlash] }/{YtImgFileName(size) }";
        }

        private static string YtImgFileName(int size) => size switch
        {
            <= 90 => "default.jpg",
            <= 180 => "mqdefault.jpg",
            <= 360 => "hqdefault.jpg",
            _ => "maxresdefault.jpg"
        };

        private static bool IsSizeSuffix(ReadOnlySpan<char> suffix) =>
            suffix.Length > 1 && (suffix[0] is 'w' or 's' or 'h') && char.IsDigit(suffix[1] );
    }
}
