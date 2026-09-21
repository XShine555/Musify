import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import {
	createApiClient,
	requireAccessTokenAction,
	requireUser,
	unwrapOrError
} from '$lib/server/api';
import { ALBUMS_PAGE_SIZE } from '$lib/config';
import { parseAlbumForm } from '$lib/server/albumForm';
import { uploadPresignedImage } from '$lib/server/upload';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const result = await api.GET('/albums/users/{userId}', {
		params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: ALBUMS_PAGE_SIZE } }
	});

	const albums = unwrapOrError(result, 'No se pudieron cargar tus álbumes.');

	return { albums };
};

export const actions: Actions = {
	create: async ({ request, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;

		const form = await request.formData();
		const parsed = parseAlbumForm(form);
		if ('failMessage' in parsed) return fail(400, { message: parsed.failMessage });

		const cover = form.get('cover');
		if (!(cover instanceof File) || cover.size === 0) {
			return fail(400, { message: 'La portada es obligatoria.' });
		}

		const api = createApiClient({ fetch, accessToken });

		const uploaded = await uploadPresignedImage(
			(args) => api.POST('/albums/upload-picture', { body: args }),
			cover
		);
		if ('failMessage' in uploaded) {
			return fail(502, { message: uploaded.failMessage, detail: uploaded.detail });
		}

		const { data, error: err } = await api.POST('/albums', {
			body: { ...parsed.body, pictureIntentId: uploaded.intentId }
		});

		if (err || !data) return fail(502, { message: 'No se pudo crear el álbum.' });

		redirect(303, `/albums/${data.id}`);
	}
};
