import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import { apiFor, unwrapOrError } from '$lib/server/api';
import { optionalUser } from '$lib/server/guards';
import { formFile, formString } from '$lib/server/forms/fields';
import { authedAction } from '$lib/server/actions/authedAction';
import { toAlbum, toTrack } from '$lib/server/mappers';
import { ALBUM_TRACKS_PAGE_SIZE, LIBRARY_PICKER_PAGE_SIZE } from '$lib/config';
import { parseAlbumForm } from '$lib/server/forms/albumForm';
import { deleteAlbum, syncAlbumTracks, updateAlbum, uploadAlbumCover } from '$lib/server/albums';

export const load: PageServerLoad = async ({ params, locals, url, fetch, parent }) => {
	const { allowAnonymousListening } = await parent();
	const user = optionalUser(locals, url, allowAnonymousListening);
	const api = apiFor({ fetch, locals });

	const [albumRes, tracksRes, libraryRes] = await Promise.all([
		api.GET('/albums/{id}', { params: { path: { id: params.id } } }),
		api.GET('/albums/{id}/tracks', {
			params: {
				path: { id: params.id },
				query: { pageNumber: 1, pageSize: ALBUM_TRACKS_PAGE_SIZE }
			}
		}),
		user
			? api.GET('/tracks/users/{userId}', {
					params: {
						path: { userId: user.sub },
						query: { pageNumber: 1, pageSize: LIBRARY_PICKER_PAGE_SIZE }
					}
				})
			: Promise.resolve(null)
	]);

	const album = toAlbum(unwrapOrError(albumRes, 'Álbum no encontrado.', 404));

	const tracks = (tracksRes.data?.items ?? []).map(toTrack);
	const inAlbum = new Set(tracks.map((track) => track.id));
	const library = (libraryRes?.data?.items ?? [])
		.filter((track) => !inAlbum.has(track.id))
		.map(toTrack);

	const isOwner = user !== null && String(album.ownerUserId) === user.sub;

	return { album, tracks, library, isOwner, section: isOwner ? '/albums' : null };
};

export const actions: Actions = {
	addTrack: authedAction(async ({ api, form, params }) => {
		const trackId = formString(form, 'trackId');
		if (!trackId) return fail(400, { message: 'Falta la canción.' });

		return (await syncAlbumTracks(api, params.id, [trackId], [])) ?? { added: true };
	}),

	removeTrack: authedAction(async ({ api, form, params }) => {
		const trackId = formString(form, 'trackId');
		if (!trackId) return fail(400, { message: 'Falta la canción.' });

		return (await syncAlbumTracks(api, params.id, [], [trackId])) ?? { removed: true };
	}),

	edit: authedAction(async ({ api, form, params }) => {
		const parsed = parseAlbumForm(form);
		if ('failMessage' in parsed) return fail(400, { message: parsed.failMessage });

		const cover = await uploadAlbumCover(api, formFile(form, 'cover'));
		if ('failMessage' in cover) {
			return fail(502, { message: cover.failMessage, detail: cover.detail });
		}

		return (await updateAlbum(api, params.id, parsed.body, cover.intentId)) ?? { edited: true };
	}),

	delete: authedAction(async ({ api, params }) => {
		const failure = await deleteAlbum(api, params.id);
		if (failure) return failure;
		redirect(303, '/albums');
	})
};
