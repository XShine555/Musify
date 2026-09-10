import type { RequestHandler } from './$types';
import { json } from '@sveltejs/kit';
import { createApiClient, unwrapOrError } from '$lib/server/api';

export const GET: RequestHandler = async ({ params, locals, fetch }) => {
	// No hard login gate here: the API itself allows an anonymous request when
	// AllowAnonymousListening is on, and rejects it with 401 otherwise.
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });
	const result = await api.GET('/tracks/{id}/stream', {
		params: { path: { id: params.id } }
	});

	return json(unwrapOrError(result, 'No se pudo reproducir esta canción.'));
};
