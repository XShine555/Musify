import type { PageServerLoad, Actions } from './$types';
import { error, fail } from '@sveltejs/kit';
import { createApiClient } from '$lib/server/api';

const PAGE_SIZE = 27;

export const load: PageServerLoad = async ({ url, locals, fetch }) => {
	const query = url.searchParams.get('q')?.trim() ?? '';
	const page = Math.max(1, Number(url.searchParams.get('page')) || 1);

	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const tracksPromise = api.GET('/tracks', {
		params: { query: { name: query || undefined, pageNumber: page, pageSize: PAGE_SIZE } }
	});

	const searchPromise =
		query && locals.accessToken
			? api.GET('/youtube/search', { params: { query: { query } } })
			: Promise.resolve(null);

	const playlistsPromise = locals.user
		? api.GET('/playlists/users/{userId}', {
				params: { path: { userId: locals.user.sub }, query: { pageNumber: 1, pageSize: 50 } }
			})
		: Promise.resolve(null);

	const [tracksRes, searchRes, playlistsRes] = await Promise.all([
		tracksPromise,
		searchPromise,
		playlistsPromise
	]);

	if (tracksRes.error || !tracksRes.data) {
		error(502, 'No se pudieron cargar las canciones.');
	}

	return {
		query,
		tracks: tracksRes.data,
		ytResults: searchRes?.data ?? null,
		ytError: Boolean(searchRes?.error),
		playlists: playlistsRes?.data?.items ?? [],
		needsAuth: !locals.user
	};
};

export const actions: Actions = {
	addTrack: async ({ request, locals, fetch }) => {
		if (!locals.accessToken) return fail(401, { message: 'Inicia sesión.' });

		const form = await request.formData();
		const playlistId = String(form.get('playlistId') ?? '');
		const trackId = String(form.get('trackId') ?? '');

		if (!playlistId || !trackId) {
			return fail(400, { message: 'Faltan datos de la canción.' });
		}

		const api = createApiClient({ fetch, accessToken: locals.accessToken });
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
	},

	addYouTubeToPlaylist: async ({ request, locals, fetch }) => {
		if (!locals.accessToken) return fail(401, { message: 'Inicia sesión.' });

		const form = await request.formData();
		const playlistId = String(form.get('playlistId') ?? '');
		const videoId = String(form.get('videoId') ?? '');
		const title = String(form.get('title') ?? '');
		const artist = String(form.get('artist') ?? '');
		const durationSeconds = Number(form.get('durationSeconds') ?? 0);
		const thumbnailUrl = String(form.get('thumbnailUrl') ?? '');

		if (!playlistId || !videoId || !title) {
			return fail(400, { message: 'Faltan datos de la canción.' });
		}

		const api = createApiClient({ fetch, accessToken: locals.accessToken });
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
};
