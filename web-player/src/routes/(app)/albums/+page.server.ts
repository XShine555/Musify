import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import {
	albumCoverTrackIds,
	createApiClient,
	requireAccessTokenAction,
	requireUser,
	unwrapOrError
} from '$lib/server/api';
import { ALBUMS_PAGE_SIZE } from '$lib/config';
import { parseAlbumForm } from '$lib/server/albumForm';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const result = await api.GET('/albums/users/{userId}', {
		params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: ALBUMS_PAGE_SIZE } }
	});

	const albums = unwrapOrError(result, 'No se pudieron cargar tus álbumes.');
	const trackIds = await albumCoverTrackIds(
		api,
		albums.items.map((album) => album.id)
	);

	return { albums, trackIds };
};

export const actions: Actions = {
	create: async ({ request, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;

		const parsed = parseAlbumForm(await request.formData());
		if ('failMessage' in parsed) return fail(400, { message: parsed.failMessage });

		const api = createApiClient({ fetch, accessToken });
		const { data, error: err } = await api.POST('/albums', { body: parsed.body });

		if (err || !data) return fail(502, { message: 'No se pudo crear el álbum.' });

		redirect(303, `/albums/${data.id}`);
	}
};
