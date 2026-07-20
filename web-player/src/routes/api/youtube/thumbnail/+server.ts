import type { RequestHandler } from './$types';
import { error } from '@sveltejs/kit';
import { loadThumbnail, parseThumbnailUrl } from '$lib/server/thumbnails';
import { thumbnailConfig } from '$lib/server/config';
import { THUMBNAIL_SIZES } from '$lib/thumbnails';

export const GET: RequestHandler = async ({ url, request, setHeaders }) => {
	const source = parseThumbnailUrl(url.searchParams.get('u') ?? '');
	if (!source) error(400, 'Miniatura no permitida.');

	const requested = Number(url.searchParams.get('size'));
	const size =
		Number.isInteger(requested) && requested > 0
			? Math.min(requested, thumbnailConfig.maxSize)
			: THUMBNAIL_SIZES.large;

	const image = await loadThumbnail(source, size);
	if (!image) error(404, 'Miniatura no disponible.');

	setHeaders({
		'Cache-Control': `public, max-age=${thumbnailConfig.maxAgeSeconds}, immutable`,
		'Content-Type': image.contentType,
		ETag: image.etag
	});

	if (request.headers.get('if-none-match') === image.etag) {
		return new Response(null, { status: 304 });
	}

	return new Response(image.body);
};
