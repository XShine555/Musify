import { fail, type RequestEvent } from '@sveltejs/kit';
import { createApiClient, requireAccessTokenAction } from '$lib/server/api';
import { ALBUM_TRACKS_PAGE_SIZE } from '$lib/config';

export async function addTrackAction({ request, params, locals, fetch }: RequestEvent) {
	const accessToken = requireAccessTokenAction(locals);
	if (typeof accessToken !== 'string') return accessToken;

	const form = await request.formData();
	const playlistId = String(form.get('playlistId') ?? '') || String(params.id ?? '');
	const trackId = String(form.get('trackId') ?? '');

	if (!playlistId || !trackId) {
		return fail(400, { message: 'Faltan datos de la canción.' });
	}

	const api = createApiClient({ fetch, accessToken });
	const { error: err } = await api.POST('/playlists/{id}/tracks', {
		params: { path: { id: playlistId } },
		body: { trackIds: [trackId] }
	});

	if (err) {
		return fail(502, { message: 'No se pudo añadir la canción.', trackId });
	}

	return { added: true, trackId, playlistId };
}

export async function addAlbumToPlaylistAction({ request, params, locals, fetch }: RequestEvent) {
	const accessToken = requireAccessTokenAction(locals);
	if (typeof accessToken !== 'string') return accessToken;

	const form = await request.formData();
	const playlistId = String(form.get('playlistId') ?? '') || String(params.id ?? '');
	const albumId = String(form.get('albumId') ?? '');

	if (!playlistId || !albumId) {
		return fail(400, { message: 'Faltan datos del álbum.' });
	}

	const api = createApiClient({ fetch, accessToken });
	const { data: tracks, error: tracksErr } = await api.GET('/albums/{id}/tracks', {
		params: { path: { id: albumId }, query: { pageNumber: 1, pageSize: ALBUM_TRACKS_PAGE_SIZE } }
	});

	if (tracksErr || !tracks) {
		return fail(502, { message: 'No se pudo cargar el álbum.', albumId });
	}

	const trackIds = tracks.items.map((track) => String(track.id));
	if (trackIds.length === 0) {
		return { addedAlbum: true, albumId, playlistId, added: 0 };
	}

	const { error: addErr } = await api.POST('/playlists/{id}/tracks', {
		params: { path: { id: playlistId } },
		body: { trackIds }
	});
	if (addErr) {
		return fail(502, { message: 'No se pudo añadir el álbum.', albumId });
	}

	return { addedAlbum: true, albumId, playlistId, added: trackIds.length };
}
