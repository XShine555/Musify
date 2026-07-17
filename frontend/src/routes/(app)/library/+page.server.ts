import type { PageServerLoad, Actions } from './$types';
import { error, fail } from '@sveltejs/kit';
import { createApiClient } from '$lib/server/api';

const PAGE_SIZE = 50;

export const load: PageServerLoad = async ({ locals, fetch }) => {
	if (!locals.user) error(401, 'Inicia sesión.');

	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
	const { data, error: err } = await api.GET('/tracks/users/{userId}', {
		params: { path: { userId: locals.user.sub }, query: { pageNumber: 1, pageSize: PAGE_SIZE } }
	});

	if (err || !data) error(502, 'No se pudo cargar tu biblioteca.');

	return { tracks: data };
};

export const actions: Actions = {
	deleteTrack: async ({ request, locals, fetch }) => {
		if (!locals.accessToken) return fail(401, { message: 'Inicia sesión.' });
		const trackId = String((await request.formData()).get('trackId') ?? '');
		if (!trackId) return fail(400, { message: 'Falta la canción.' });

		const api = createApiClient({ fetch, accessToken: locals.accessToken });
		const { error: err } = await api.DELETE('/tracks/{trackId}', {
			params: { path: { trackId } }
		});
		if (err) return fail(502, { message: 'No se pudo borrar la canción.' });
		return { deleted: true };
	}
};
