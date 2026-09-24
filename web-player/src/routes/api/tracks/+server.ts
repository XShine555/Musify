import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { apiFor, unwrapOrError } from '$lib/server/api';
import { toPage, toTrack } from '$lib/server/mappers';
import { EXPLORE_PAGE_SIZE, MAX_PAGE_SIZE } from '$lib/config';

export const GET: RequestHandler = async ({ url, locals, fetch }) => {
	const name = url.searchParams.get('name')?.trim() || undefined;
	const genre = url.searchParams.get('genre')?.trim() || undefined;
	const pageNumber = Math.max(1, Number(url.searchParams.get('pageNumber')) || 1);
	const pageSize = Math.min(
		MAX_PAGE_SIZE,
		Math.max(1, Number(url.searchParams.get('pageSize')) || EXPLORE_PAGE_SIZE)
	);

	const api = apiFor({ fetch, locals });
	const result = await api.GET('/tracks', {
		params: { query: { name, genre, pageNumber, pageSize } }
	});

	return json(
		toPage(unwrapOrError(result, 'No se pudieron cargar las canciones.'), (item) =>
			toTrack(item.track)
		)
	);
};
