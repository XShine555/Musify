import type { PageServerLoad, Actions } from './$types';
import { fail } from '@sveltejs/kit';
import { toDatedTrack, toPage } from '$lib/server/mappers';
import {
	createApiClient,
	requireAccessTokenAction,
	requireUser,
	unwrapOrError,
	unwrapOrFail
} from '$lib/server/api';

const PAGE_SIZE = 50;

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
	const result = await api.GET('/tracks/users/{userId}', {
		params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: PAGE_SIZE } }
	});

	return {
		tracks: toPage(unwrapOrError(result, 'No se pudo cargar tu biblioteca.'), toDatedTrack)
	};
};

export const actions: Actions = {
	deleteTrack: async ({ request, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;
		const trackId = String((await request.formData()).get('trackId') ?? '');
		if (!trackId) return fail(400, { message: 'Falta la canción.' });

		const api = createApiClient({ fetch, accessToken });
		const result = await api.DELETE('/tracks/{trackId}', {
			params: { path: { trackId } }
		});
		const failure = unwrapOrFail(result, 'No se pudo borrar la canción.');
		if (failure) return failure;
		return { deleted: true };
	}
};
