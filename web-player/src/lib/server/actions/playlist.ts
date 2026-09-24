import { fail } from '@sveltejs/kit';
import { formString } from '$lib/server/forms/fields';
import { authedAction } from '$lib/server/actions/authedAction';

export const addTrackAction = authedAction(async ({ api, form, params }) => {
	const playlistId = formString(form, 'playlistId') || (params.id ?? '');
	const trackId = formString(form, 'trackId');

	if (!playlistId || !trackId) {
		return fail(400, { message: 'Faltan datos de la canción.' });
	}

	const { error } = await api.POST('/playlists/{id}/tracks', {
		params: { path: { id: playlistId } },
		body: { trackIds: [trackId] }
	});

	if (error) {
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

	const { data, error } = await api.POST('/playlists/{id}/albums/{albumId}', {
		params: { path: { id: playlistId, albumId } }
	});

	if (error || !data) {
		return fail(502, { message: 'No se pudo añadir el álbum.', albumId });
	}

	const added = Number(data.addedCount);
	return { addedAlbum: true, albumId, playlistId, added };
});
