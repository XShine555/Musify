import type { PageServerLoad, Actions } from './$types';
import { createApiClient, requireUser, unwrapOrError } from '$lib/server/api';
import {
	addYouTubeToPlaylistAction,
	addYoutubeAlbumToPlaylistAction
} from '$lib/server/playlistActions';

export const load: PageServerLoad = async ({ params, locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const [albumRes, playlistsRes] = await Promise.all([
		api.GET('/albums/external/{source}/{externalId}', {
			params: { path: { source: params.source, externalId: params.externalId } }
		}),
		api.GET('/playlists/users/{userId}', {
			params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: 50 } }
		})
	]);

	const album = unwrapOrError(albumRes, 'No se pudo cargar el álbum.', 404);

	return { album, playlists: playlistsRes.data?.items ?? [], section: null };
};

export const actions: Actions = {
	addYouTubeToPlaylist: addYouTubeToPlaylistAction,
	addYoutubeAlbumToPlaylist: addYoutubeAlbumToPlaylistAction
};
