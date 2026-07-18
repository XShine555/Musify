import { fail, type RequestEvent } from '@sveltejs/kit';
import { createApiClient, requireAccessTokenAction } from '$lib/server/api';

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
	const { error: err, response } = await api.POST('/playlists/{playlistId}/tracks/{trackId}', {
		params: { path: { playlistId, trackId } }
	});

	if (err) {
		if (response?.status === 409) {
			return fail(409, { message: 'La canción ya está en esa playlist.', trackId });
		}
		return fail(502, { message: 'No se pudo añadir la canción.', trackId });
	}

	return { added: true, trackId, playlistId };
}

export async function addYouTubeToPlaylistAction({
	request,
	params,
	locals,
	fetch
}: RequestEvent) {
	const accessToken = requireAccessTokenAction(locals);
	if (typeof accessToken !== 'string') return accessToken;

	const form = await request.formData();
	const playlistId = String(form.get('playlistId') ?? '') || String(params.id ?? '');
	const videoId = String(form.get('videoId') ?? '');
	const title = String(form.get('title') ?? '');
	const artist = String(form.get('artist') ?? '');
	const durationSeconds = Number(form.get('durationSeconds') ?? 0);
	const thumbnailUrl = String(form.get('thumbnailUrl') ?? '');

	if (!playlistId || !videoId || !title) {
		return fail(400, { message: 'Faltan datos de la canción.' });
	}

	const api = createApiClient({ fetch, accessToken });
	const { error: err, response } = await api.POST('/playlists/{playlistId}/youtube-tracks', {
		params: { path: { playlistId } },
		body: { videoId, title, artist, durationSeconds, thumbnailUrl }
	});

	if (err) {
		if (response?.status === 409) {
			return fail(409, { message: 'La canción ya está en esa playlist.', videoId });
		}
		return fail(502, { message: 'No se pudo añadir la canción.', videoId });
	}

	return { added: true, videoId, playlistId };
}
