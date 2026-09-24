import type { PageServerLoad, Actions } from './$types';
import { createApiClient, requireUser, unwrapOrError } from '$lib/server/api';
import { toMix } from '$lib/server/mappers';
import { addTrackAction } from '$lib/server/playlistActions';

export const load: PageServerLoad = async ({ params, locals, url, fetch }) => {
	requireUser(locals, url);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const mixRes = await api.GET('/mixes/{id}', { params: { path: { id: params.id } } });

	return { mix: toMix(unwrapOrError(mixRes, 'Mezcla no encontrada.', 404)) };
};

export const actions: Actions = {
	addTrack: addTrackAction
};
