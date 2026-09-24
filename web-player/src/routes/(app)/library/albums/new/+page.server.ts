import type { Actions, PageServerLoad } from './$types';
import { apiFor } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { loadLibraryTracks } from '$lib/server/albums';
import { saveAlbum } from '$lib/server/actions/libraryAlbum';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	return { tracks: await loadLibraryTracks(api, user.sub) };
};

export const actions: Actions = { saveAlbum };
