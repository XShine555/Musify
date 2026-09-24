import { fail, isActionFailure, redirect } from '@sveltejs/kit';
import type { Album } from '$lib/types';
import { toAlbum, toLibraryTrack, toPage } from '$lib/server/mappers';
import { failOnError, unwrapOrError, type ApiClient } from '$lib/server/api';
import { formFile, formString } from '$lib/server/forms/fields';
import { parseAlbumForm } from '$lib/server/forms/albumForm';
import { uploadOptionalCover } from '$lib/server/upload';
import { authedAction, type ActionContext } from '$lib/server/actions/authedAction';
import { ALBUMS_PAGE_SIZE, ALBUM_TRACKS_PAGE_SIZE, LIBRARY_PAGE_SIZE } from '$lib/config';

export const ALBUMS_SECTION_URL = '/library/albums';

export async function trackIdsByAlbum(
	api: ApiClient,
	albums: Album[]
): Promise<Record<string, string[]>> {
	const entries = await Promise.all(
		albums.map(async (album) => {
			const tracks = await api.GET('/albums/{id}/tracks', {
				params: {
					path: { id: album.id },
					query: { pageNumber: 1, pageSize: ALBUM_TRACKS_PAGE_SIZE }
				}
			});
			return [album.id, (tracks.data?.items ?? []).map((track) => track.id)] as const;
		})
	);
	return Object.fromEntries(entries);
}

function albumTitleByTrackId(albums: Album[], trackIds: Record<string, string[]>) {
	return new Map(
		albums.flatMap((album) => (trackIds[album.id] ?? []).map((id) => [id, album.title] as const))
	);
}

export async function loadUserAlbums(api: ApiClient, userId: string) {
	const result = await api.GET('/albums/users/{userId}', {
		params: { path: { userId }, query: { pageNumber: 1, pageSize: ALBUMS_PAGE_SIZE } }
	});
	return toPage(unwrapOrError(result, 'No se pudieron cargar tus álbumes.'), toAlbum);
}

export async function loadLibraryTracks(api: ApiClient, userId: string) {
	const [tracksResult, albums] = await Promise.all([
		api.GET('/tracks/users/{userId}', {
			params: { path: { userId }, query: { pageNumber: 1, pageSize: LIBRARY_PAGE_SIZE } }
		}),
		loadUserAlbums(api, userId)
	]);
	const albumTitles = albumTitleByTrackId(albums.items, await trackIdsByAlbum(api, albums.items));
	return toPage(unwrapOrError(tracksResult, 'No se pudo cargar tu biblioteca.'), (dto) =>
		toLibraryTrack(dto, albumTitles.get(dto.id) ?? null)
	);
}

export function formIds(form: FormData, key: string): string[] {
	return formString(form, key).split(',').filter(Boolean);
}

export function tagged<T>(action: string, handler: (context: ActionContext) => Promise<T>) {
	return authedAction(async (context) => {
		const result = await handler(context);
		return isActionFailure(result)
			? fail(result.status, { ...(result.data as Record<string, unknown> | undefined), action })
			: { ...result, action };
	});
}

export const saveAlbum = tagged('saveAlbum', async ({ api, form }) => {
	const parsed = parseAlbumForm(form);
	if ('failMessage' in parsed) return fail(400, { message: parsed.failMessage });

	const existingId = formString(form, 'albumId');
	const cover = await uploadOptionalCover(api, '/albums/upload-picture', formFile(form, 'cover'));
	if ('failMessage' in cover)
		return fail(502, { message: cover.failMessage, detail: cover.detail });

	let albumId = existingId;
	if (existingId) {
		const updated = await api.PUT('/albums/{id}', {
			params: { path: { id: existingId } },
			body: parsed.body
		});
		const failure = failOnError(updated, 'No se pudo actualizar el álbum.');
		if (failure) return failure;

		if (cover.intentId) {
			const picture = await api.PUT('/albums/{id}/picture', {
				params: { path: { id: existingId } },
				body: { pictureIntentId: cover.intentId }
			});
			const pictureFailure = failOnError(picture, 'No se pudo actualizar la portada.');
			if (pictureFailure) return pictureFailure;
		}
	} else {
		if (!cover.intentId) return fail(400, { message: 'La portada es obligatoria.' });
		const created = await api.POST('/albums', {
			body: { ...parsed.body, pictureIntentId: cover.intentId }
		});
		if (created.error || !created.data) return fail(502, { message: 'No se pudo crear el álbum.' });
		albumId = created.data.id;
	}

	const addIds = formIds(form, 'addTrackIds');
	if (addIds.length > 0) {
		const added = await api.POST('/albums/{id}/tracks', {
			params: { path: { id: albumId } },
			body: { trackIds: addIds }
		});
		const failure = failOnError(added, 'No se pudieron añadir las canciones.');
		if (failure) return failure;
	}

	const removed = await Promise.all(
		formIds(form, 'removeTrackIds').map((trackId) =>
			api.DELETE('/albums/{id}/tracks/{trackId}', { params: { path: { id: albumId, trackId } } })
		)
	);
	const removeFailed = removed.find((result) => result.error);
	const failure = removeFailed && failOnError(removeFailed, 'No se pudieron quitar las canciones.');
	return failure ?? { saved: true };
});

export const deleteAlbum = tagged('deleteAlbum', async ({ api, form }) => {
	const albumId = formString(form, 'albumId');
	if (!albumId) return fail(400, { message: 'Falta el álbum.' });

	const result = await api.DELETE('/albums/{id}', { params: { path: { id: albumId } } });
	const failure = failOnError(result, 'No se pudo eliminar el álbum.');
	if (failure) return failure;
	redirect(303, ALBUMS_SECTION_URL);
});
