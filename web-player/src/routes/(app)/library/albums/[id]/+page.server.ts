import type { Actions, PageServerLoad } from './$types';
import { apiFor, unwrapOrError } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { toAlbum } from '$lib/server/mappers';
import { deleteAlbum, loadLibraryTracks, saveAlbum, trackIdsByAlbum } from '$lib/server/albums';

export const load: PageServerLoad = async ({ params, locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	const albumResult = await api.GET('/albums/{id}', { params: { path: { id: params.id } } });
	const album = toAlbum(unwrapOrError(albumResult, 'Álbum no encontrado.', 404));
	const [tracks, memberIds] = await Promise.all([
		loadLibraryTracks(api, user.sub),
		trackIdsByAlbum(api, [album])
	]);
	return { album, tracks, memberIds: memberIds[album.id] ?? [] };
};

export const actions: Actions = { saveAlbum, deleteAlbum };
