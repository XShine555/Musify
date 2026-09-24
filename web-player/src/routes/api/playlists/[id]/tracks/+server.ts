import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { createApiClient, unwrapOrError } from '$lib/server/api';
import { toPage, toTrack } from '$lib/server/mappers';
import { PLAYLIST_TRACKS_PAGE_SIZE } from '$lib/config';

export const GET: RequestHandler = async ({ params, locals, fetch }) => {
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
	const result = await api.GET('/playlists/{playlistId}/tracks', {
		params: {
			path: { playlistId: params.id },
			query: { pageNumber: 1, pageSize: PLAYLIST_TRACKS_PAGE_SIZE }
		}
	});

	return json(toPage(unwrapOrError(result, 'No se pudo cargar la playlist.'), toTrack));
};
