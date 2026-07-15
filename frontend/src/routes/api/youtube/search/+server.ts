import type { RequestHandler } from './$types';
import { json, error } from '@sveltejs/kit';
import { createApiClient } from '$lib/server/api';

export const GET: RequestHandler = async ({ url, locals, fetch }) => {
	if (!locals.accessToken) {
		error(401, 'Inicia sesión para buscar.');
	}

	const query = url.searchParams.get('query')?.trim() ?? '';
	if (!query) {
		error(400, 'Falta el término de búsqueda.');
	}

	const continuation = url.searchParams.get('continuation') ?? undefined;

	const api = createApiClient({ fetch, accessToken: locals.accessToken });
	const { data, error: err, response } = await api.GET('/youtube/search', {
		params: { query: { query, continuation } }
	});

	if (err || !data) {
		error(response?.status ?? 502, 'No se pudo buscar en YouTube Music.');
	}

	return json(data);
};
