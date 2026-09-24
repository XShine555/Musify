import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { apiFor } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { loadLibraryTracks } from '$lib/server/libraryTracks';
import { parseTrackFilter } from '$lib/data/libraryTracks';

export const GET: RequestHandler = async ({ url, locals, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	const pageNumber = Math.max(1, Number(url.searchParams.get('pageNumber')) || 1);
	const tracks = await loadLibraryTracks(api, user.sub, {
		q: url.searchParams.get('q')?.trim() ?? '',
		filter: parseTrackFilter(url.searchParams.get('status')),
		pageNumber
	});
	return json(tracks);
};
