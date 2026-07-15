import type { PageServerLoad, Actions } from './$types';
import { error, fail, redirect } from '@sveltejs/kit';
import { createApiClient } from '$lib/server/api';
import { putPresigned } from '$lib/server/upload';

const MAX_NAME = 100;

const IMAGE_TYPES: Record<string, string> = {
	jpg: 'image/jpeg',
	jpeg: 'image/jpeg',
	png: 'image/png',
	webp: 'image/webp'
};

function extOf(name: string): string {
	const dot = name.lastIndexOf('.');
	return dot >= 0 ? name.slice(dot + 1).toLowerCase() : '';
}

function contentTypeOf(file: File): string {
	if (file.type) return file.type;
	return IMAGE_TYPES[extOf(file.name)] ?? 'application/octet-stream';
}

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
		const cover = form.get('cover');
		if (name === '' || name.length > MAX_NAME) {
			return fail(400, { message: 'El nombre es obligatorio (máx. 100 caracteres).' });
		}

		const api = createApiClient({ fetch, accessToken: locals.accessToken });

		let newPictureIntentId: string | null = null;
		if (cover instanceof File && cover.size > 0) {
			const contentType = contentTypeOf(cover);
			const { data: upload, error: uploadError } = await api.POST('/playlists/upload-picture', {
				body: {
					fileType: extOf(cover.name) || 'jpg',
					contentType,
					expectedSizeBytes: cover.size
				}
			});

			if (uploadError || !upload) {
				return fail(502, { message: 'No se pudo reservar la subida de la portada.' });
			}

			try {
				const coverBuffer = await cover.arrayBuffer();
				await putPresigned(upload.uploadUrl, Buffer.from(coverBuffer), upload.contentType);
			} catch (err) {
				return fail(502, {
					message: 'Falló la subida de la portada al almacenamiento.',
					detail: err instanceof Error ? err.message : undefined
				});
			}

			newPictureIntentId = upload.intentId;
		}

		const { error: err } = await api.PUT('/playlists/{playlistId}', {
			params: { path: { playlistId: params.id } },
			body: { newName: name, newDescription: description, newPictureIntentId }
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
