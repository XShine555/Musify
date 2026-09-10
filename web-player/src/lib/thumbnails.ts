export const THUMBNAIL_SIZES = {
	small: 128,
	medium: 192,
	large: 360
} as const;

export type ThumbnailSize = keyof typeof THUMBNAIL_SIZES;

const PROXY_PATH = '/api/thumbnail';

export function thumbnailSrc(
	url: string | undefined,
	size: ThumbnailSize = 'large'
): string | undefined {
	if (!url) return undefined;
	if (!url.startsWith('http')) return url;

	return `${PROXY_PATH}?u=${encodeURIComponent(url)}&size=${THUMBNAIL_SIZES[size]}`;
}
