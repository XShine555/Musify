import { fail } from '@sveltejs/kit';
import { authedAction, formString } from '$lib/server/api';
import { ALBUM_TRACKS_PAGE_SIZE } from '$lib/config';

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

	const { data: tracks, error: tracksError } = await api.GET('/albums/{albumId}/tracks', {
		params: { path: { albumId }, query: { pageNumber: 1, pageSize: ALBUM_TRACKS_PAGE_SIZE } }
	});

	if (tracksError || !tracks) {
		return fail(502, { message: 'No se pudo cargar el álbum.', albumId });
	}

	let added = 0;
	for (const track of tracks.items) {
		const { error } = await api.POST('/playlists/{playlistId}/tracks', {
			params: { path: { playlistId } },
			body: { trackId: track.id }
		});
		if (!error) added++;
	}

	return { addedAlbum: true, albumId, playlistId, added };
});
