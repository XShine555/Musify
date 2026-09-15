import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { createApiClient, unwrapOrError } from '$lib/server/api';
import { EXPLORE_PAGE_SIZE } from '$lib/config';

export const GET: RequestHandler = async ({ url, locals, fetch }) => {
	const name = url.searchParams.get('name')?.trim() || undefined;
	const pageNumber = Math.max(1, Number(url.searchParams.get('pageNumber')) || 1);
	const pageSize = Math.max(1, Number(url.searchParams.get('pageSize')) || EXPLORE_PAGE_SIZE);

	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
	const result = await api.GET('/tracks', {
		params: { query: { name, pageNumber, pageSize } }
	});

	return json(unwrapOrError(result, 'No se pudieron cargar las canciones.'));
};
