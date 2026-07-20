export const THUMBNAIL_SIZES = {
	small: 96,
	medium: 160,
	large: 320
} as const;

export type ThumbnailSize = keyof typeof THUMBNAIL_SIZES;

const PROXY_PATH = '/api/youtube/thumbnail';

export function thumbnailSrc(
	url: string | undefined,
	size: ThumbnailSize = 'large'
): string | undefined {
	if (!url) return undefined;
	if (!url.startsWith('http')) return url;

	return `${PROXY_PATH}?u=${encodeURIComponent(url)}&size=${THUMBNAIL_SIZES[size]}`;
}
