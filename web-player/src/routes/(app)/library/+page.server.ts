import type { PageServerLoad, Actions } from './$types';
import { fail, isActionFailure } from '@sveltejs/kit';
import { apiFor } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { formIds, formString } from '$lib/server/forms/fields';
import { tagged } from '$lib/server/actions/tagged';
import { loadLibraryTracks, loadStatusCounts } from '$lib/server/libraryTracks';
import { deleteTracks, setTracksPublished } from '$lib/server/tracks';
import { parseTrackFilter } from '$lib/data/libraryTracks';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	const q = url.searchParams.get('q')?.trim() ?? '';
	const filter = parseTrackFilter(url.searchParams.get('status'));

	const [tracks, counts] = await Promise.all([
		loadLibraryTracks(api, user.sub, { q, filter }),
		loadStatusCounts(api, user.sub)
	]);
	return { tracks, counts, q, filter };
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
	}),

	setPublished: tagged('setPublished', async ({ api, form }) => {
		const trackIds = formIds(form, 'trackIds');
		if (trackIds.length === 0) return fail(400, { message: 'Falta la canción.' });

		const isPublished = formString(form, 'isPublished') === 'true';
		const result = await setTracksPublished(api, trackIds, isPublished);
		if (isActionFailure(result)) return result;
		return { ...result, isPublished };
	})
};
