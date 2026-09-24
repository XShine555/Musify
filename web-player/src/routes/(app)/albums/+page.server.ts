import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import { apiFor, unwrapOrError } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { formFile } from '$lib/server/forms/fields';
import { authedAction } from '$lib/server/actions/authedAction';
import { toAlbum, toPage } from '$lib/server/mappers';
import { ALBUMS_PAGE_SIZE } from '$lib/config';
import { parseAlbumForm } from '$lib/server/forms/albumForm';
import { createAlbum, uploadAlbumCover } from '$lib/server/albums';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });

	const result = await api.GET('/albums/users/{userId}', {
		params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: ALBUMS_PAGE_SIZE } }
	});

	const albums = toPage(unwrapOrError(result, 'No se pudieron cargar tus álbumes.'), toAlbum);

	return { albums };
};

export const actions: Actions = {
	create: authedAction(async ({ api, form }) => {
		const parsed = parseAlbumForm(form);
		if ('failMessage' in parsed) return fail(400, { message: parsed.failMessage });

		const cover = await uploadAlbumCover(api, formFile(form, 'cover'));
		if ('failMessage' in cover) {
			return fail(502, { message: cover.failMessage, detail: cover.detail });
		}
		if (!cover.intentId) return fail(400, { message: 'La portada es obligatoria.' });

		const created = await createAlbum(api, parsed.body, cover.intentId);
		if ('failMessage' in created) return fail(502, { message: created.failMessage });

		redirect(303, `/albums/${created.id}`);
	})
};
