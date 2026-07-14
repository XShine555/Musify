import type { PageServerLoad, Actions } from './$types';
import { error, fail, redirect } from '@sveltejs/kit';
import { createApiClient } from '$lib/server/api';

const MAX_NAME = 100;

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

		if (name === '' || name.length > MAX_NAME) {
			return fail(400, { message: 'El nombre es obligatorio (máx. 100 caracteres).', name, description });
		}

		const api = createApiClient({ fetch, accessToken: locals.accessToken });
		const { data, error: err } = await api.POST('/playlists', {
			body: { name, description, pictureIntentId: null }
		});

		if (err || !data) return fail(502, { message: 'No se pudo crear la playlist.', name, description });

		redirect(303, `/playlists/${data.id}`);
	}
};
