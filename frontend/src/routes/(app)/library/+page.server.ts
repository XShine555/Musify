import type { PageServerLoad } from './$types';
import { error } from '@sveltejs/kit';
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
