import type { RequestHandler } from './$types';
import { json, error } from '@sveltejs/kit';
import { createApiClient, requireAccessToken, unwrapOrError } from '$lib/server/api';

export const GET: RequestHandler = async ({ url, locals, fetch }) => {
	const accessToken = requireAccessToken(locals, 'Inicia sesión para buscar.');

	const query = url.searchParams.get('query')?.trim() ?? '';
	if (!query) {
		error(400, 'Falta el término de búsqueda.');
	}

	const continuation = url.searchParams.get('continuation') ?? undefined;

	const api = createApiClient({ fetch, accessToken });
	const result = await api.GET('/youtube/search', {
		params: { query: { query, continuation } }
	});

	return json(unwrapOrError(result, 'No se pudo buscar en YouTube Music.'));
};
