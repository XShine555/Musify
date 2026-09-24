import { toLibraryTrack, toPage, toStatusCounts } from '$lib/server/mappers';
import { unwrapOrError, type ApiClient } from '$lib/server/api';
import { filterQuery, type TrackFilter } from '$lib/data/libraryTracks';
import { LIBRARY_PAGE_SIZE } from '$lib/config';

export interface LibraryTracksQuery {
	q?: string;
	filter?: TrackFilter;
	pageNumber?: number;
	pageSize?: number;
}

export async function loadLibraryTracks(
	api: ApiClient,
	userId: string,
	{ q, filter = 'all', pageNumber = 1, pageSize = LIBRARY_PAGE_SIZE }: LibraryTracksQuery = {}
) {
	const { status, published } = filterQuery(filter);
	const result = await api.GET('/tracks/users/{userId}', {
		params: {
			path: { userId },
			query: {
				name: q || undefined,
				status: status ? [...status] : undefined,
				published,
				pageNumber,
				pageSize
			}
		}
	});
	return toPage(unwrapOrError(result, 'No se pudo cargar tu biblioteca.'), toLibraryTrack);
}

export async function loadStatusCounts(api: ApiClient, userId: string) {
	const result = await api.GET('/tracks/users/{userId}/status-counts', {
		params: { path: { userId } }
	});
	return toStatusCounts(unwrapOrError(result, 'No se pudieron cargar los contadores.'));
}
