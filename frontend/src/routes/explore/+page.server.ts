import type { PageServerLoad } from './$types';
import { error } from '@sveltejs/kit';
import { createApiClient } from '$lib/server/api';

const PAGE_SIZE = 24;

export const load: PageServerLoad = async ({ url, locals, fetch }) => {
	const query = url.searchParams.get('q')?.trim() ?? '';
	const page = Math.max(1, Number(url.searchParams.get('page')) || 1);

	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
	const { data, error: err } = await api.GET('/tracks', {
		params: { query: { name: query || undefined, pageNumber: page, pageSize: PAGE_SIZE } }
	});

	if (err || !data) {
		error(502, 'No se pudieron cargar las canciones.');
	}

	return { tracks: data, query };
};
