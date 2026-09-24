import type { PageServerLoad, Actions } from './$types';
import { fail } from '@sveltejs/kit';
import { toDatedTrack, toPage } from '$lib/server/mappers';
import { apiFor, failOnError, unwrapOrError } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { formString } from '$lib/server/forms/fields';
import { authedAction } from '$lib/server/actions/authedAction';
import { LIBRARY_PAGE_SIZE } from '$lib/config';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	const result = await api.GET('/tracks/users/{userId}', {
		params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: LIBRARY_PAGE_SIZE } }
	});

	return {
		tracks: toPage(unwrapOrError(result, 'No se pudo cargar tu biblioteca.'), toDatedTrack)
	};
};

export const actions: Actions = {
	deleteTrack: authedAction(async ({ api, form }) => {
		const trackId = formString(form, 'trackId');
		if (!trackId) return fail(400, { message: 'Falta la canción.' });

		const result = await api.DELETE('/tracks/{trackId}', {
			params: { path: { trackId } }
		});
		return failOnError(result, 'No se pudo borrar la canción.') ?? { deleted: true };
	})
};
