import type { PageServerLoad, Actions } from './$types';
import { error, fail, redirect } from '@sveltejs/kit';
import { createApiClient } from '$lib/server/api';

const MAX_NAME = 100;

export const load: PageServerLoad = async ({ params, locals, fetch }) => {
	if (!locals.user) error(401, 'Inicia sesión.');

	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const [playlistRes, tracksRes, libraryRes] = await Promise.all([
		api.GET('/playlists/{id}', { params: { path: { id: params.id } } }),
		api.GET('/playlists/{playlistId}/tracks', {
			params: { path: { playlistId: params.id }, query: { pageNumber: 1, pageSize: 200 } }
		}),
		api.GET('/tracks/users/{userId}', {
			params: { path: { userId: locals.user.sub }, query: { pageNumber: 1, pageSize: 200 } }
		})
	]);

	if (playlistRes.error || !playlistRes.data) error(404, 'Playlist no encontrada.');

	const tracks = tracksRes.data?.items ?? [];
	const inPlaylist = new Set(tracks.map((t) => t.id));
	const library = (libraryRes.data?.items ?? []).filter((t) => !inPlaylist.has(t.id));

	return { playlist: playlistRes.data, tracks, library };
};

export const actions: Actions = {
	addTrack: async ({ request, params, locals, fetch }) => {
		if (!locals.accessToken) return fail(401, { message: 'Inicia sesión.' });
		const trackId = String((await request.formData()).get('trackId') ?? '');
		if (!trackId) return fail(400, { message: 'Falta la canción.' });

		const api = createApiClient({ fetch, accessToken: locals.accessToken });
		const { error: err } = await api.POST('/playlists/{playlistId}/tracks/{trackId}', {
			params: { path: { playlistId: params.id, trackId } }
		});
		if (err) return fail(502, { message: 'No se pudo añadir la canción.' });
		return { added: true };
	},

	removeTrack: async ({ request, params, locals, fetch }) => {
		if (!locals.accessToken) return fail(401, { message: 'Inicia sesión.' });
		const trackId = String((await request.formData()).get('trackId') ?? '');
		if (!trackId) return fail(400, { message: 'Falta la canción.' });

		const api = createApiClient({ fetch, accessToken: locals.accessToken });
		const { error: err } = await api.DELETE('/playlists/{playlistId}/tracks/{trackId}', {
			params: { path: { playlistId: params.id, trackId } }
		});
		if (err) return fail(502, { message: 'No se pudo quitar la canción.' });
		return { removed: true };
	},

	rename: async ({ request, params, locals, fetch }) => {
		if (!locals.accessToken) return fail(401, { message: 'Inicia sesión.' });
		const form = await request.formData();
		const name = String(form.get('name') ?? '').trim();
		const description = String(form.get('description') ?? '').trim();
		if (name === '' || name.length > MAX_NAME) {
			return fail(400, { message: 'El nombre es obligatorio (máx. 100 caracteres).' });
		}

		const api = createApiClient({ fetch, accessToken: locals.accessToken });
		const { error: err } = await api.PUT('/playlists/{playlistId}', {
			params: { path: { playlistId: params.id } },
			body: { newName: name, newDescription: description, newPictureIntentId: null }
		});
		if (err) return fail(502, { message: 'No se pudo actualizar la playlist.' });
		return { renamed: true };
	},

	delete: async ({ params, locals, fetch }) => {
		if (!locals.accessToken) return fail(401, { message: 'Inicia sesión.' });

		const api = createApiClient({ fetch, accessToken: locals.accessToken });
		const { error: err } = await api.DELETE('/playlists/{playlistId}', {
			params: { path: { playlistId: params.id } }
		});
		if (err) return fail(502, { message: 'No se pudo eliminar la playlist.' });
		redirect(303, '/playlists');
	}
};
