import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import {
	createApiClient,
	requireAccessTokenAction,
	requireUser,
	unwrapOrError
} from '$lib/server/api';
import { addYouTubeToPlaylistAction } from '$lib/server/playlistActions';

export const load: PageServerLoad = async ({ params, locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const [albumRes, playlistsRes] = await Promise.all([
		api.GET('/youtube/albums/{albumId}', { params: { path: { albumId: params.albumId } } }),
		api.GET('/playlists/users/{userId}', {
			params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: 50 } }
		})
	]);

	const album = unwrapOrError(albumRes, 'No se pudo cargar el álbum de YouTube Music.', 404);

	return { album, playlists: playlistsRes.data?.items ?? [] };
};

export const actions: Actions = {
	addYouTubeToPlaylist: addYouTubeToPlaylistAction,

	save: async ({ params, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;

		const api = createApiClient({ fetch, accessToken });
		const {
			data,
			error: err,
			response
		} = await api.POST('/albums/youtube/{albumId}', {
			params: { path: { albumId: params.albumId } }
		});

		if (response?.status === 409) {
			return fail(409, { message: 'Ese álbum ya está en tu biblioteca.' });
		}
		if (err || !data) {
			return fail(502, { message: 'No se pudo guardar el álbum.' });
		}

		redirect(303, `/albums/${data.id}`);
	}
};
