import type { Actions, PageServerLoad } from './$types';
import { apiFor, unwrapOrError } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { toAlbum } from '$lib/server/mappers';
import { albumTrackIds } from '$lib/server/albums';
import { loadLibraryTracks } from '$lib/server/libraryTracks';
import { removeAlbum, saveAlbum } from '$lib/server/actions/libraryAlbum';
import { LIBRARY_PICKER_PAGE_SIZE } from '$lib/config';

export const load: PageServerLoad = async ({ params, locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	const albumResult = await api.GET('/albums/{id}', { params: { path: { id: params.id } } });
	const album = toAlbum(unwrapOrError(albumResult, 'Álbum no encontrado.', 404));
	const [tracks, memberIds] = await Promise.all([
		loadLibraryTracks(api, user.sub, { pageSize: LIBRARY_PICKER_PAGE_SIZE }),
		albumTrackIds(api, album.id)
	]);
	return { album, tracks, memberIds };
};

export const actions: Actions = { saveAlbum, deleteAlbum: removeAlbum };
