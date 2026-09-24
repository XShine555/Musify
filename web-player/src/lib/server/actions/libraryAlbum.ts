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

	let albumId = existingId;
	if (existingId) {
		const failure = await updateAlbum(api, existingId, parsed.body, cover.intentId);
		if (failure) return failure;
	} else {
		if (!cover.intentId) return fail(400, { message: 'La portada es obligatoria.' });
		const created = await createAlbum(api, parsed.body, cover.intentId);
		if ('failMessage' in created) return fail(502, { message: created.failMessage });
		albumId = created.id;
	}

	const failure = await syncAlbumTracks(
		api,
		albumId,
		formIds(form, 'addTrackIds'),
		formIds(form, 'removeTrackIds')
	);
	if (failure && !existingId) {
		return fail(502, {
			message: 'El álbum se creó, pero no se pudieron añadir las canciones. Inténtalo de nuevo.',
			albumId
		});
	}
	return failure ?? { saved: true };
});

export const removeAlbum = tagged('deleteAlbum', async ({ api, form }) => {
	const albumId = formString(form, 'albumId');
	if (!albumId) return fail(400, { message: 'Falta el álbum.' });

	const failure = await deleteAlbum(api, albumId);
	if (failure) return failure;
	redirect(303, ALBUMS_SECTION_URL);
});
