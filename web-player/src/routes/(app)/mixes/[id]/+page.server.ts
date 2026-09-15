import type { PageServerLoad, Actions } from './$types';
import { createApiClient, requireUser, unwrapOrError } from '$lib/server/api';
import { addTrackAction } from '$lib/server/playlistActions';
import { PLAYLIST_PICKER_PAGE_SIZE } from '$lib/config';

export const load: PageServerLoad = async ({ params, locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const [mixRes, playlistsRes] = await Promise.all([
		api.GET('/mixes/{id}', { params: { path: { id: params.id } } }),
		api.GET('/playlists/users/{userId}', {
			params: {
				path: { userId: user.sub },
				query: { pageNumber: 1, pageSize: PLAYLIST_PICKER_PAGE_SIZE }
			}
		})
	]);

	const mix = unwrapOrError(mixRes, 'Mezcla no encontrada.', 404);

	return { mix, playlists: playlistsRes.data?.items ?? [] };
};

export const actions: Actions = {
	addTrack: addTrackAction
};
