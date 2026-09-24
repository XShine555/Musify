import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import {
	createApiClient,
	optionalUser,
	requireAccessTokenAction,
	unwrapOrError,
	unwrapOrFail
} from '$lib/server/api';
import { toDatedTrack, toPlaylist } from '$lib/server/mappers';
import { uploadPresignedImage } from '$lib/server/upload';

const MAX_NAME = 100;

export const load: PageServerLoad = async ({ params, locals, url, fetch, parent }) => {
	const { allowAnonymousListening } = await parent();
	const user = optionalUser(locals, url, allowAnonymousListening);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const [playlistRes, tracksRes] = await Promise.all([
		api.GET('/playlists/{id}', { params: { path: { id: params.id } } }),
		api.GET('/playlists/{playlistId}/tracks', {
			params: { path: { playlistId: params.id }, query: { pageNumber: 1, pageSize: 200 } }
		})
	]);

	const playlist = toPlaylist(unwrapOrError(playlistRes, 'Playlist no encontrada.', 404));
	const tracks = (tracksRes.data?.items ?? []).map(toDatedTrack);

	const isOwner = user !== null && String(playlist.ownerUserId) === user.sub;

	return { playlist, tracks, isOwner, section: isOwner ? '/playlists' : null };
};

export const actions: Actions = {
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
		const newVisibility = form.get('visibility') === 'public' ? 'Public' : 'Private';
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
			body: { newName: name, newDescription: description, newPictureIntentId, newVisibility }
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
