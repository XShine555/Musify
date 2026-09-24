import type { PageServerLoad, Actions } from './$types';
import { fail } from '@sveltejs/kit';
import { apiFor } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { formIds } from '$lib/server/forms/fields';
import { tagged } from '$lib/server/actions/tagged';
import { loadLibraryTracks } from '$lib/server/albums';
import { deleteTracks } from '$lib/server/tracks';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	return { tracks: await loadLibraryTracks(api, user.sub) };
};

export const actions: Actions = {
	deleteTracks: tagged('deleteTracks', async ({ api, form }) => {
		const trackIds = formIds(form, 'trackIds');
		if (trackIds.length === 0) return fail(400, { message: 'Falta la canción.' });

		const { failed } = await deleteTracks(api, trackIds);
		if (failed === 0) return { deleted: true };
		return fail(502, {
			message:
				trackIds.length === 1
					? 'No se pudo borrar la canción.'
					: `No se pudieron borrar ${failed} de ${trackIds.length} canciones.`
		});
	})
};
