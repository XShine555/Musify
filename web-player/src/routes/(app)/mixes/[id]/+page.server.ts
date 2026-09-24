import type { PageServerLoad, Actions } from './$types';
import { apiFor, unwrapOrError } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { toMix } from '$lib/server/mappers';
import { addTrackAction } from '$lib/server/actions/playlist';

export const load: PageServerLoad = async ({ params, locals, url, fetch }) => {
	requireUser(locals, url);
	const api = apiFor({ fetch, locals });

	const mixRes = await api.GET('/mixes/{id}', { params: { path: { id: params.id } } });

	return { mix: toMix(unwrapOrError(mixRes, 'Mezcla no encontrada.', 404)) };
};

export const actions: Actions = {
	addTrack: addTrackAction
};
