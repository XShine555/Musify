import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { createApiClient, requireAccessToken, unwrapOrError } from '$lib/server/api';

export const GET: RequestHandler = async ({ params, locals, fetch }) => {
	const accessToken = requireAccessToken(locals, 'Inicia sesión para reproducir.');

	const api = createApiClient({ fetch, accessToken });
	const result = await api.GET('/tracks/{id}/stream', {
		params: { path: { id: params.id } }
	});

	return json(unwrapOrError(result, 'No se pudo obtener el stream de la pista.'));
};
