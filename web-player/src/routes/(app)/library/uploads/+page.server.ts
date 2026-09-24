import type { PageServerLoad } from './$types';
import { apiFor } from '$lib/server/api';
import { loadUserAlbums } from '$lib/server/albums';
import { requireUser } from '$lib/server/guards';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	const [genres, albums] = await Promise.all([
		api.GET('/genres/available'),
		loadUserAlbums(api, user.sub)
	]);
	return {
		genres: genres.data ?? [],
		albums: albums.items.map(({ id, title }) => ({ id, title }))
	};
};
