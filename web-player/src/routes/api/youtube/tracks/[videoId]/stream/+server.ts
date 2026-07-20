import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { createApiClient, requireAccessToken, unwrapOrError } from '$lib/server/api';

export const GET: RequestHandler = async ({ params, locals, fetch }) => {
	const accessToken = requireAccessToken(locals, 'Inicia sesión para reproducir.');

	const api = createApiClient({ fetch, accessToken });
	const result = await api.GET('/youtube/tracks/{videoId}/stream', {
		params: { path: { videoId: params.videoId } }
	});

	return json(unwrapOrError(result, 'No se pudo obtener el stream de YouTube.'));
};
