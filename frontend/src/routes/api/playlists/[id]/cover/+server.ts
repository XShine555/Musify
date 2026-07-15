import type { RequestHandler } from './$types';
import { apiConfig } from '$lib/server/config';

const ALLOWED = new Set(['small', 'medium', 'large']);

export const GET: RequestHandler = async ({ params, url, fetch }) => {
	const size = url.searchParams.get('size') ?? 'medium';
	const safeSize = ALLOWED.has(size) ? size : 'medium';

	const upstream = await fetch(
		`${apiConfig.baseUrl}/playlists/${params.id}/cover?size=${safeSize}`
	);

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
};
