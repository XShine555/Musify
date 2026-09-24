import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { createApiClient, unwrapOrError } from '$lib/server/api';
import { ALBUM_TRACKS_PAGE_SIZE } from '$lib/config';

export const GET: RequestHandler = async ({ params, locals, fetch }) => {
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
	const result = await api.GET('/albums/{id}/tracks', {
		params: {
			path: { id: params.id },
			query: { pageNumber: 1, pageSize: ALBUM_TRACKS_PAGE_SIZE }
		}
	});

	return json(unwrapOrError(result, 'No se pudo cargar el álbum.'));
};
