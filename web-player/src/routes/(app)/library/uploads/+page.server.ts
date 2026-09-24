import type { PageServerLoad } from './$types';
import { apiFor } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	const genres = await api.GET('/genres/available');
	return { genres: genres.data ?? [] };
};
