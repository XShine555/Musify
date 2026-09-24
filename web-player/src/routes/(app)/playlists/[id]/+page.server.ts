import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import {
	apiFor,
	authedAction,
	failOnError,
	formFile,
	formString,
	optionalUser,
	unwrapOrError
} from '$lib/server/api';
import { PLAYLIST_TRACKS_PAGE_SIZE } from '$lib/config';
import { toDatedTrack, toPlaylist } from '$lib/server/mappers';
import { uploadPresignedImage } from '$lib/server/upload';

const MAX_NAME = 100;

export const load: PageServerLoad = async ({ params, locals, url, fetch, parent }) => {
	const { allowAnonymousListening } = await parent();
	const user = optionalUser(locals, url, allowAnonymousListening);
	const api = apiFor({ fetch, locals });

	const [playlistRes, tracksRes] = await Promise.all([
		api.GET('/playlists/{id}', { params: { path: { id: params.id } } }),
		api.GET('/playlists/{playlistId}/tracks', {
			params: {
				path: { playlistId: params.id },
				query: { pageNumber: 1, pageSize: PLAYLIST_TRACKS_PAGE_SIZE }
			}
		})
	]);

	const playlist = toPlaylist(unwrapOrError(playlistRes, 'Playlist no encontrada.', 404));
	const tracks = (tracksRes.data?.items ?? []).map(toDatedTrack);

	const isOwner = user !== null && String(playlist.ownerUserId) === user.sub;

	return { playlist, tracks, isOwner, section: isOwner ? '/playlists' : null };
};

export const actions: Actions = {
	removeTrack: authedAction(async ({ api, form, params }) => {
		const trackId = formString(form, 'trackId');
		if (!trackId) return fail(400, { message: 'Falta la canción.' });

		const result = await api.DELETE('/playlists/{playlistId}/tracks/{trackId}', {
			params: { path: { playlistId: params.id, trackId } }
		});
		return failOnError(result, 'No se pudo quitar la canción.') ?? { removed: true };
	}),

	edit: authedAction(async ({ api, form, params }) => {
		const name = formString(form, 'name').trim();
		const description = formString(form, 'description').trim();
		const newVisibility = formString(form, 'visibility') === 'public' ? 'Public' : 'Private';
		if (name === '' || name.length > MAX_NAME) {
			return fail(400, { message: 'El nombre es obligatorio (máx. 100 caracteres).' });
		}

		let newPictureIntentId: string | null = null;
		const cover = formFile(form, 'cover');
		if (cover) {
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
			body: { newName: name, newDescription: description, newPictureIntentId, newVisibility }
		});
		return failOnError(result, 'No se pudo actualizar la playlist.') ?? { edited: true };
	}),

	delete: authedAction(async ({ api, params }) => {
		const result = await api.DELETE('/playlists/{playlistId}', {
			params: { path: { playlistId: params.id } }
		});
		const failure = failOnError(result, 'No se pudo eliminar la playlist.');
		if (failure) return failure;
		redirect(303, '/playlists');
	})
};
