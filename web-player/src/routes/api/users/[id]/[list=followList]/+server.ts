import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { createApiClient, unwrapOrError } from '$lib/server/api';
import { fetchFollowList } from '$lib/server/follows';
import { FOLLOW_LIST_PAGE_SIZE } from '$lib/config';

export const GET: RequestHandler = async ({ params, url, locals, fetch }) => {
	const pageNumber = Math.max(1, Number(url.searchParams.get('pageNumber')) || 1);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
	const result = await fetchFollowList(
		api,
		params.list,
		params.id,
		pageNumber,
		FOLLOW_LIST_PAGE_SIZE
	);

	return json(unwrapOrError(result, 'No se pudo cargar la lista.', result.response?.status));
};
