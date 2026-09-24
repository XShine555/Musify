import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import { apiFor, authedAction, formFile, requireUser, unwrapOrError } from '$lib/server/api';
import { toAlbum, toPage } from '$lib/server/mappers';
import { ALBUMS_PAGE_SIZE } from '$lib/config';
import { parseAlbumForm } from '$lib/server/forms/albumForm';
import { uploadPresignedImage } from '$lib/server/upload';

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

		const cover = formFile(form, 'cover');
		if (!cover) return fail(400, { message: 'La portada es obligatoria.' });

		const uploaded = await uploadPresignedImage(
			(args) => api.POST('/albums/upload-picture', { body: args }),
			cover
		);
		if ('failMessage' in uploaded) {
			return fail(502, { message: uploaded.failMessage, detail: uploaded.detail });
		}

		const { data, error } = await api.POST('/albums', {
			body: { ...parsed.body, pictureIntentId: uploaded.intentId }
		});

		if (error || !data) return fail(502, { message: 'No se pudo crear el álbum.' });

		redirect(303, `/albums/${data.id}`);
	})
};
