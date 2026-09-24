import type { PageServerLoad } from './$types';
import { apiFor } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { loadUserAlbums } from '$lib/server/albums';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	return { albums: await loadUserAlbums(api, user.sub) };
};
