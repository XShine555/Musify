import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import {
	createApiClient,
	requireAccessTokenAction,
	requireUser,
	unwrapOrError,
	unwrapOrFail
} from '$lib/server/api';
import { uploadPresignedImage } from '$lib/server/upload';
import { fetchYoutubeFiller, shuffle } from '$lib/server/youtube';
import { addTrackAction, addYouTubeToPlaylistAction } from '$lib/server/playlistActions';
import type { YouTubeSong } from '$lib/types';

const MAX_NAME = 100;
const SUGGESTIONS_LIMIT = 20;

export const load: PageServerLoad = async ({ params, locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const [playlistRes, tracksRes, libraryRes, playlistsRes] = await Promise.all([
		api.GET('/playlists/{id}', { params: { path: { id: params.id } } }),
		api.GET('/playlists/{playlistId}/tracks', {
			params: { path: { playlistId: params.id }, query: { pageNumber: 1, pageSize: 200 } }
		}),
		api.GET('/tracks/users/{userId}', {
			params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: 50 } }
		}),
		api.GET('/playlists/users/{userId}', {
			params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: 50 } }
		})
	]);

	const playlist = unwrapOrError(playlistRes, 'Playlist no encontrada.', 404);

	const tracks = tracksRes.data?.items ?? [];
	const inPlaylist = new Set(tracks.map((t) => t.id));
	const library = (libraryRes.data?.items ?? []).filter((t) => !inPlaylist.has(t.id));

	const alreadyLinked = new Set(
		tracks.filter((t) => t.source === 'YouTube' && t.externalId).map((t) => t.externalId)
	);
	const seeds = [...new Set(tracks.map((t) => t.artist).filter((a) => !!a))];
	const seed = seeds.length > 0 ? shuffle(seeds)[0] : undefined;

	const youtube = fetchYoutubeFiller(
		api,
		locals.accessToken,
		SUGGESTIONS_LIMIT + alreadyLinked.size,
		seed
	)
		.then((filler) => ({
			query: filler.query,
			items: (filler.items as YouTubeSong[])
				.filter((song) => !alreadyLinked.has(song.videoId))
				.slice(0, SUGGESTIONS_LIMIT)
		}))
		.catch(() => ({ query: '', items: [] as YouTubeSong[] }));

	return {
		playlist,
		tracks,
		library,
		youtube,
		playlists: playlistsRes.data?.items ?? []
	};
};

export const actions: Actions = {
	addTrack: addTrackAction,
	addYouTubeToPlaylist: addYouTubeToPlaylistAction,

	removeTrack: async ({ request, params, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;
		const trackId = String((await request.formData()).get('trackId') ?? '');
		if (!trackId) return fail(400, { message: 'Falta la canción.' });

		const api = createApiClient({ fetch, accessToken });
		const result = await api.DELETE('/playlists/{playlistId}/tracks/{trackId}', {
			params: { path: { playlistId: params.id, trackId } }
		});
		const failure = unwrapOrFail(result, 'No se pudo quitar la canción.');
		if (failure) return failure;
		return { removed: true };
	},

	rename: async ({ request, params, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;
		const form = await request.formData();
		const name = String(form.get('name') ?? '').trim();
		const description = String(form.get('description') ?? '').trim();
		const cover = form.get('cover');
		if (name === '' || name.length > MAX_NAME) {
			return fail(400, { message: 'El nombre es obligatorio (máx. 100 caracteres).' });
		}

		const api = createApiClient({ fetch, accessToken });

		let newPictureIntentId: string | null = null;
		if (cover instanceof File && cover.size > 0) {
			const result = await uploadPresignedImage(
				(args) => api.POST('/playlists/upload-picture', { body: args }),
				cover
			);
			if ('failMessage' in result) {
				return fail(502, { message: result.failMessage, detail: result.detail });
			}
			newPictureIntentId = result.intentId;
		}

		const result = await api.PUT('/playlists/{playlistId}', {
			params: { path: { playlistId: params.id } },
			body: { newName: name, newDescription: description, newPictureIntentId }
		});
		const failure = unwrapOrFail(result, 'No se pudo actualizar la playlist.');
		if (failure) return failure;
		return { renamed: true };
	},

	delete: async ({ params, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;

		const api = createApiClient({ fetch, accessToken });
		const result = await api.DELETE('/playlists/{playlistId}', {
			params: { path: { playlistId: params.id } }
		});
		const failure = unwrapOrFail(result, 'No se pudo eliminar la playlist.');
		if (failure) return failure;
		redirect(303, '/playlists');
	}
};
