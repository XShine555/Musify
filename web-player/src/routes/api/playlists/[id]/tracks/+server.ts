import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { apiFor } from '$lib/server/api';
import { toTrack } from '$lib/server/mappers';
import { fetchAllPages } from '$lib/server/pagination';
import { PLAYLIST_TRACKS_LIMIT } from '$lib/config';

export const GET: RequestHandler = async ({ params, locals, fetch }) => {
	const api = apiFor({ fetch, locals });
	const tracks = await fetchAllPages(
		async (pageNumber, pageSize) =>
			(
				await api.GET('/playlists/{id}/tracks', {
					params: { path: { id: params.id }, query: { pageNumber, pageSize } }
				})
			).data,
		PLAYLIST_TRACKS_LIMIT
	);

	return json({
		items: tracks.map(toTrack),
		pageNumber: 1,
		hasNextPage: false,
		totalItemCount: tracks.length
	});
};
