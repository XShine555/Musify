import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { apiFor, unwrapOrError } from '$lib/server/api';
import { toPage, toTrack } from '$lib/server/mappers';
import { ALBUM_TRACKS_PAGE_SIZE } from '$lib/config';

export const GET: RequestHandler = async ({ params, locals, fetch }) => {
	const api = apiFor({ fetch, locals });
	const result = await api.GET('/albums/{albumId}/tracks', {
		params: {
			path: { albumId: params.id },
			query: { pageNumber: 1, pageSize: ALBUM_TRACKS_PAGE_SIZE }
		}
	});

	return json(toPage(unwrapOrError(result, 'No se pudo cargar el álbum.'), toTrack));
};
