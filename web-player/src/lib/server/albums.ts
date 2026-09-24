import type { Album } from '$lib/types';
import { toAlbum, toLibraryTrack, toPage } from '$lib/server/mappers';
import { failOnError, unwrapOrError, type ApiClient } from '$lib/server/api';
import { uploadOptionalCover } from '$lib/server/upload';
import type { AlbumFormBody } from '$lib/server/forms/albumForm';
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

export type CoverUpload = { intentId: string | null } | { failMessage: string; detail?: string };

export function uploadAlbumCover(api: ApiClient, file: File | null): Promise<CoverUpload> {
	return uploadOptionalCover(api, '/albums/upload-picture', file);
}

export async function createAlbum(
	api: ApiClient,
	body: AlbumFormBody,
	pictureIntentId: string
): Promise<{ id: string } | { failMessage: string }> {
	const created = await api.POST('/albums', { body: { ...body, pictureIntentId } });
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
