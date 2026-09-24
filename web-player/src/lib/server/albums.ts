import { toAlbum, toPage } from '$lib/server/mappers';
import { failOnError, unwrapOrError, type ApiClient } from '$lib/server/api';
import { uploadOptionalCover } from '$lib/server/upload';
import type { AlbumFormBody } from '$lib/server/forms/albumForm';
import { ALBUMS_PAGE_SIZE } from '$lib/config';

export const ALBUMS_SECTION_URL = '/library/albums';

export async function albumTrackIds(api: ApiClient, albumId: string): Promise<string[]> {
	const result = await api.GET('/albums/{id}/tracks/ids', { params: { path: { id: albumId } } });
	return unwrapOrError(result, 'No se pudieron cargar las canciones del álbum.').trackIds;
}

export async function loadUserAlbums(api: ApiClient, userId: string) {
	const result = await api.GET('/albums/users/{userId}', {
		params: { path: { userId }, query: { pageNumber: 1, pageSize: ALBUMS_PAGE_SIZE } }
	});
	return toPage(unwrapOrError(result, 'No se pudieron cargar tus álbumes.'), toAlbum);
}

export type CoverUpload = { intentId: string | null } | { failMessage: string; detail?: string };

export function uploadAlbumCover(api: ApiClient, file: File | null): Promise<CoverUpload> {
	return uploadOptionalCover(api, '/albums/upload-picture', file);
}

export async function createAlbum(
	api: ApiClient,
	body: AlbumFormBody,
	pictureIntentId: string,
	trackIds: string[] = []
): Promise<{ id: string } | { failMessage: string }> {
	const created = await api.POST('/albums', { body: { ...body, pictureIntentId, trackIds } });
	if (created.error || !created.data) return { failMessage: 'No se pudo crear el álbum.' };
	return { id: created.data.id };
}

export async function updateAlbum(
	api: ApiClient,
	id: string,
	body: AlbumFormBody,
	pictureIntentId?: string | null
) {
	const updated = await api.PUT('/albums/{id}', { params: { path: { id } }, body });
	const failure = failOnError(updated, 'No se pudo actualizar el álbum.');
	if (failure || !pictureIntentId) return failure;

	const picture = await api.PUT('/albums/{id}/picture', {
		params: { path: { id } },
		body: { pictureIntentId }
	});
	return failOnError(picture, 'No se pudo actualizar la portada.');
}

export async function syncAlbumTracks(
	api: ApiClient,
	albumId: string,
	add: string[],
	remove: string[]
) {
	if (add.length > 0) {
		const added = await api.POST('/albums/{id}/tracks', {
			params: { path: { id: albumId } },
			body: { trackIds: add }
		});
		const failure = failOnError(added, 'No se pudieron añadir las canciones.');
		if (failure) return failure;
	}

	const removed = await Promise.all(
		remove.map((trackId) =>
			api.DELETE('/albums/{id}/tracks/{trackId}', { params: { path: { id: albumId, trackId } } })
		)
	);
	const removeFailed = removed.find((result) => result.error);
	return removeFailed && failOnError(removeFailed, 'No se pudieron quitar las canciones.');
}

export async function deleteAlbum(api: ApiClient, id: string) {
	const result = await api.DELETE('/albums/{id}', { params: { path: { id } } });
	return failOnError(result, 'No se pudo eliminar el álbum.');
}
