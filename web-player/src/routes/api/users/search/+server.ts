import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { apiFor } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { searchUsers } from '$lib/server/coauthors';
import { SEARCH_MIN_LENGTH } from '$lib/config';

export const GET: RequestHandler = async ({ url, locals, fetch }) => {
	requireUser(locals, url);
	const query = url.searchParams.get('q')?.trim() ?? '';
	if (query.length < SEARCH_MIN_LENGTH) return json([]);
	return json(await searchUsers(apiFor({ fetch, locals }), query));
};
