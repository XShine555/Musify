import { apiConfig } from '$lib/server/config';

const ALLOWED_SIZES = new Set(['small', 'medium', 'large']);

export async function proxyCoverImage(
	fetch: typeof globalThis.fetch,
	kind: 'playlists' | 'tracks',
	id: string,
	size: string | null
): Promise<Response> {
	const safeSize = size && ALLOWED_SIZES.has(size) ? size : 'medium';

	const upstream = await fetch(`${apiConfig.baseUrl}/${kind}/${id}/cover?size=${safeSize}`);

	if (!upstream.ok) {
		return new Response(null, { status: upstream.status });
	}

	return new Response(upstream.body, {
		status: 200,
		headers: {
			'Content-Type': upstream.headers.get('content-type') ?? 'image/webp',
			'Cache-Control': 'public, max-age=86400'
		}
	});
}
