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

export const load: PageServerLoad = async ({ locals, fetch }) => {
	if (!locals.user) error(401, 'Inicia sesión.');

	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
	const { data, error: err } = await api.GET('/playlists/users/{userId}', {
		params: { path: { userId: locals.user.sub }, query: { pageNumber: 1, pageSize: 50 } }
	});

	if (err || !data) error(502, 'No se pudieron cargar tus playlists.');

	const covers = await Promise.all(
		data.items.map(async (playlist) => {
			const { data: tracks } = await api.GET('/playlists/{playlistId}/tracks', {
				params: { path: { playlistId: playlist.id }, query: { pageNumber: 1, pageSize: 4 } }
			});
			return [playlist.id, tracks?.items.map((t) => t.id) ?? []] as const;
		})
	);

	return { playlists: data, trackIds: Object.fromEntries(covers) as Record<string, string[]> };
};

export const actions: Actions = {
	create: async ({ request, locals, fetch }) => {
		if (!locals.accessToken) return fail(401, { message: 'Inicia sesión.' });

		const form = await request.formData();
		const name = String(form.get('name') ?? '').trim();
		const description = String(form.get('description') ?? '').trim();
		const cover = form.get('cover');

		if (name === '' || name.length > MAX_NAME) {
			return fail(400, { message: 'El nombre es obligatorio (máx. 100 caracteres).', name, description });
		}

		const api = createApiClient({ fetch, accessToken: locals.accessToken });

		let pictureIntentId: string | null = null;
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
				return fail(502, { message: 'No se pudo reservar la subida de la portada.', name, description });
			}

			try {
				const coverBuffer = await cover.arrayBuffer();
				await putPresigned(upload.uploadUrl, Buffer.from(coverBuffer), upload.contentType);
			} catch (err) {
				return fail(502, {
					message: 'Falló la subida de la portada al almacenamiento.',
					name,
					description,
					detail: err instanceof Error ? err.message : undefined
				});
			}

			pictureIntentId = upload.intentId;
		}

		const { data, error: err } = await api.POST('/playlists', {
			body: { name, description, pictureIntentId }
		});

		if (err || !data) return fail(502, { message: 'No se pudo crear la playlist.', name, description });

		redirect(303, `/playlists/${data.id}`);
	}
};
