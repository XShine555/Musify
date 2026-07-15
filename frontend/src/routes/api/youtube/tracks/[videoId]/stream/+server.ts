import type { RequestHandler } from './$types';
import { json, error } from '@sveltejs/kit';
import { createApiClient } from '$lib/server/api';

export const GET: RequestHandler = async ({ params, locals, fetch }) => {
	if (!locals.accessToken) {
		error(401, 'Inicia sesión para reproducir.');
	}

	const api = createApiClient({ fetch, accessToken: locals.accessToken });
	const { data, error: err, response } = await api.GET('/youtube/tracks/{videoId}/stream', {
		params: { path: { videoId: params.videoId } }
	});

	if (err || !data) {
		error(response?.status ?? 502, 'No se pudo obtener el stream de YouTube.');
	}

	return json(data);
};
