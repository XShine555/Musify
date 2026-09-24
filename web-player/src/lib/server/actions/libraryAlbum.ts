import { fail, redirect } from '@sveltejs/kit';
import { formFile, formIds, formString } from '$lib/server/forms/fields';
import { parseAlbumForm } from '$lib/server/forms/albumForm';
import { tagged } from '$lib/server/actions/tagged';
import {
	ALBUMS_SECTION_URL,
	createAlbum,
	deleteAlbum,
	syncAlbumTracks,
	updateAlbum,
	uploadAlbumCover
} from '$lib/server/albums';

export const saveAlbum = tagged('saveAlbum', async ({ api, form }) => {
	const parsed = parseAlbumForm(form);
	if ('failMessage' in parsed) return fail(400, { message: parsed.failMessage });

	const existingId = formString(form, 'albumId');
	const cover = await uploadAlbumCover(api, formFile(form, 'cover'));
	if ('failMessage' in cover)
		return fail(502, { message: cover.failMessage, detail: cover.detail });

	const addIds = formIds(form, 'addTrackIds');
	if (!existingId) {
		if (!cover.intentId) return fail(400, { message: 'La portada es obligatoria.' });
		const created = await createAlbum(api, parsed.body, cover.intentId, addIds);
		if ('failMessage' in created) return fail(502, { message: created.failMessage });
		return { saved: true };
	}

	const updateFailure = await updateAlbum(api, existingId, parsed.body, cover.intentId);
	if (updateFailure) return updateFailure;

	const failure = await syncAlbumTracks(api, existingId, addIds, formIds(form, 'removeTrackIds'));
	return failure ?? { saved: true };
});

export const removeAlbum = tagged('deleteAlbum', async ({ api, form }) => {
	const albumId = formString(form, 'albumId');
	if (!albumId) return fail(400, { message: 'Falta el álbum.' });

	const failure = await deleteAlbum(api, albumId);
	if (failure) return failure;
	redirect(303, ALBUMS_SECTION_URL);
});
