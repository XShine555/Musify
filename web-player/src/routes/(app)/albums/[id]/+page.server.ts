import type { PageServerLoad, Actions } from './$types';
import { fail, redirect } from '@sveltejs/kit';
import {
	apiFor,
	authedAction,
	failOnError,
	formFile,
	formString,
	optionalUser,
	unwrapOrError
} from '$lib/server/api';
import { toAlbum, toTrack } from '$lib/server/mappers';
import { ALBUM_TRACKS_PAGE_SIZE, LIBRARY_PICKER_PAGE_SIZE } from '$lib/config';
import { parseAlbumForm } from '$lib/server/albumForm';
import { uploadPresignedImage } from '$lib/server/upload';

export const load: PageServerLoad = async ({ params, locals, url, fetch, parent }) => {
	const { allowAnonymousListening } = await parent();
	const user = optionalUser(locals, url, allowAnonymousListening);
	const api = apiFor({ fetch, locals });

	const [albumRes, tracksRes, libraryRes] = await Promise.all([
		api.GET('/albums/{id}', { params: { path: { id: params.id } } }),
		api.GET('/albums/{albumId}/tracks', {
			params: {
				path: { albumId: params.id },
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

		const result = await api.POST('/albums/{albumId}/tracks/{trackId}', {
			params: { path: { albumId: params.id, trackId } }
		});
		return failOnError(result, 'No se pudo añadir la canción.') ?? { added: true };
	}),

	removeTrack: authedAction(async ({ api, form, params }) => {
		const trackId = formString(form, 'trackId');
		if (!trackId) return fail(400, { message: 'Falta la canción.' });

		const result = await api.DELETE('/albums/{albumId}/tracks/{trackId}', {
			params: { path: { albumId: params.id, trackId } }
		});
		return failOnError(result, 'No se pudo quitar la canción.') ?? { removed: true };
	}),

	edit: authedAction(async ({ api, form, params }) => {
		const parsed = parseAlbumForm(form);
		if ('failMessage' in parsed) return fail(400, { message: parsed.failMessage });

		let newPictureIntentId: string | null = null;
		const cover = formFile(form, 'cover');
		if (cover) {
			const uploaded = await uploadPresignedImage(
				(args) => api.POST('/albums/upload-picture', { body: args }),
				cover
			);
			if ('failMessage' in uploaded) {
				return fail(502, { message: uploaded.failMessage, detail: uploaded.detail });
			}
			newPictureIntentId = uploaded.intentId;
		}

		const result = await api.PUT('/albums/{albumId}', {
			params: { path: { albumId: params.id } },
			body: {
				newTitle: parsed.body.title,
				newDescription: parsed.body.description,
				newReleaseYear: parsed.body.releaseYear,
				newPictureIntentId
			}
		});
		return failOnError(result, 'No se pudo actualizar el álbum.') ?? { edited: true };
	}),

	delete: authedAction(async ({ api, params }) => {
		const result = await api.DELETE('/albums/{albumId}', {
			params: { path: { albumId: params.id } }
		});
		const failure = failOnError(result, 'No se pudo eliminar el álbum.');
		if (failure) return failure;
		redirect(303, '/albums');
	})
};
