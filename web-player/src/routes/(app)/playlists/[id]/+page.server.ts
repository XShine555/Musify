import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import { apiFor, failOnError, unwrapOrError } from '$lib/server/api';
import { optionalUser } from '$lib/server/guards';
import { formString } from '$lib/server/forms/fields';
import { authedAction } from '$lib/server/actions/authedAction';
import { PLAYLIST_TRACKS_PAGE_SIZE } from '$lib/config';
import { toDatedTrack, toPlaylist } from '$lib/server/mappers';
import { parsePlaylistForm } from '$lib/server/forms/playlistForm';
import { uploadOptionalCover } from '$lib/server/upload';

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
		const parsed = parsePlaylistForm(form);
		if ('failMessage' in parsed) return fail(400, { message: parsed.failMessage });
		const { name, description, visibility, cover } = parsed.body;

		const uploaded = await uploadOptionalCover(api, '/playlists/upload-picture', cover);
		if ('failMessage' in uploaded) {
			return fail(502, { message: uploaded.failMessage, detail: uploaded.detail });
		}

		const result = await api.PUT('/playlists/{playlistId}', {
			params: { path: { playlistId: params.id } },
			body: {
				newName: name,
				newDescription: description,
				newPictureIntentId: uploaded.intentId,
				newVisibility: visibility
			}
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
