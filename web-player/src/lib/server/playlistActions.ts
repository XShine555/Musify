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
	const { error: err, response } = await api.POST('/playlists/{playlistId}/tracks', {
		params: { path: { playlistId } },
		body: { trackId, source: null, externalId: null }
	});

	if (err) {
		if (response?.status === 409) {
			return fail(409, { message: 'La canción ya está en esa playlist.', trackId });
		}
		return fail(502, { message: 'No se pudo añadir la canción.', trackId });
	}

	return { added: true, trackId, playlistId };
}

export async function addYouTubeToPlaylistAction({ request, params, locals, fetch }: RequestEvent) {
	const accessToken = requireAccessTokenAction(locals);
	if (typeof accessToken !== 'string') return accessToken;

	const form = await request.formData();
	const playlistId = String(form.get('playlistId') ?? '') || String(params.id ?? '');
	const videoId = String(form.get('videoId') ?? '');

	if (!playlistId || !videoId) {
		return fail(400, { message: 'Faltan datos de la canción.' });
	}

	const api = createApiClient({ fetch, accessToken });
	const { error: err, response } = await api.POST('/playlists/{playlistId}/tracks', {
		params: { path: { playlistId } },
		body: { trackId: null, source: 'YouTube', externalId: videoId }
	});

	if (err) {
		if (response?.status === 409) {
			return fail(409, { message: 'La canción ya está en esa playlist.', videoId });
		}
		return fail(502, { message: 'No se pudo añadir la canción.', videoId });
	}

	return { added: true, videoId, playlistId };
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
	const { data: tracks, error: tracksErr } = await api.GET('/albums/{albumId}/tracks', {
		params: { path: { albumId }, query: { pageNumber: 1, pageSize: ALBUM_TRACKS_PAGE_SIZE } }
	});

	if (tracksErr || !tracks) {
		return fail(502, { message: 'No se pudo cargar el álbum.', albumId });
	}

	let added = 0;
	for (const track of tracks.items) {
		const { error: err } = await api.POST('/playlists/{playlistId}/tracks', {
			params: { path: { playlistId } },
			body: { trackId: String(track.id), source: null, externalId: null }
		});
		if (!err) added++;
	}

	return { addedAlbum: true, albumId, playlistId, added };
}

export async function addYoutubeAlbumToPlaylistAction({
	request,
	params,
	locals,
	fetch
}: RequestEvent) {
	const accessToken = requireAccessTokenAction(locals);
	if (typeof accessToken !== 'string') return accessToken;

	const form = await request.formData();
	const playlistId = String(form.get('playlistId') ?? '') || String(params.id ?? '');
	const albumId = String(form.get('albumId') ?? '');

	if (!playlistId || !albumId) {
		return fail(400, { message: 'Faltan datos del álbum.' });
	}

	const api = createApiClient({ fetch, accessToken });
	const { data: detail, error: detailErr } = await api.GET('/albums/external/{source}/{externalId}', {
		params: { path: { source: 'YouTube', externalId: albumId } }
	});

	if (detailErr || !detail) {
		return fail(502, { message: 'No se pudo cargar el álbum de YouTube.', albumId });
	}

	let added = 0;
	for (const track of detail.tracks) {
		const { error: err } = await api.POST('/playlists/{playlistId}/tracks', {
			params: { path: { playlistId } },
			body: { trackId: null, source: 'YouTube', externalId: track.videoId }
		});
		if (!err) added++;
	}

	return { addedAlbum: true, albumId, playlistId, added };
}
