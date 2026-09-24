import type { PageServerLoad } from './$types';
import { apiFor, optionalUser, unwrapOrError } from '$lib/server/api';
import { toPage } from '$lib/server/mappers';
import { fetchFollowList } from '$lib/server/follows';
import { FOLLOW_LIST_PAGE_SIZE } from '$lib/config';

export const load: PageServerLoad = async ({ params, locals, url, fetch, parent }) => {
	const { allowAnonymousListening } = await parent();
	const viewer = optionalUser(locals, url, allowAnonymousListening);
	const api = apiFor({ fetch, locals });

	const [profileRes, listRes] = await Promise.all([
		api.GET('/users/{id}/profile', { params: { path: { id: params.id } } }),
		fetchFollowList(api, params.list, params.id, 1, FOLLOW_LIST_PAGE_SIZE)
	]);

	const profile = unwrapOrError(profileRes, 'Usuario no encontrado.', 404);
	const forbidden = listRes.response?.status === 403;

	return {
		list: params.list,
		profile,
		users: forbidden
			? null
			: toPage(unwrapOrError(listRes, 'No se pudo cargar la lista.'), (item) => item),
		forbidden,
		viewerId: viewer?.sub ?? null
	};
};
