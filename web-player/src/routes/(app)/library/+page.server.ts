import type { PageServerLoad, Actions } from './$types';
import { fail } from '@sveltejs/kit';
import { apiFor, failOnError } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { formIds, loadLibraryTracks, tagged } from '$lib/server/albums';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	return { tracks: await loadLibraryTracks(api, user.sub) };
};

export const actions: Actions = {
	deleteTracks: tagged('deleteTracks', async ({ api, form }) => {
		const trackIds = formIds(form, 'trackIds');
		if (trackIds.length === 0) return fail(400, { message: 'Falta la canción.' });

		const results = await Promise.all(
			trackIds.map((id) => api.DELETE('/tracks/{id}', { params: { path: { id } } }))
		);
		const failed = results.find((result) => result.error);
		return (
			(failed &&
				failOnError(
					failed,
					trackIds.length === 1
						? 'No se pudo borrar la canción.'
						: 'No se pudieron borrar todas las canciones.'
				)) ?? { deleted: true }
		);
	})
};
