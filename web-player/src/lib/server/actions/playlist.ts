import { fail } from '@sveltejs/kit';
import { formString } from '$lib/server/forms/fields';
import { authedAction } from '$lib/server/actions/authedAction';

export const addTrackAction = authedAction(async ({ api, form, params }) => {
	const playlistId = formString(form, 'playlistId') || (params.id ?? '');
	const trackId = formString(form, 'trackId');

	if (!playlistId || !trackId) {
		return fail(400, { message: 'Faltan datos de la canción.' });
	}

	const { error, response } = await api.POST('/playlists/{playlistId}/tracks', {
		params: { path: { playlistId } },
		body: { trackId }
	});

	if (error) {
		if (response?.status === 409) {
			return fail(409, { message: 'La canción ya está en esa playlist.', trackId });
		}
		return fail(502, { message: 'No se pudo añadir la canción.', trackId });
	}

	return { added: true, trackId, playlistId };
});

export const addAlbumToPlaylistAction = authedAction(async ({ api, form, params }) => {
	const playlistId = formString(form, 'playlistId') || (params.id ?? '');
	const albumId = formString(form, 'albumId');

	if (!playlistId || !albumId) {
		return fail(400, { message: 'Faltan datos del álbum.' });
	}

	const { data, error } = await api.POST('/playlists/{playlistId}/albums/{albumId}', {
		params: { path: { playlistId, albumId } }
	});

	if (error || !data) {
		return fail(502, { message: 'No se pudo añadir el álbum.', albumId });
	}

	const added = Number(data.addedCount);
	return { addedAlbum: true, albumId, playlistId, added };
});
