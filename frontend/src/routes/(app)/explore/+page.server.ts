import type { PageServerLoad, Actions } from './$types';
import { error, fail } from '@sveltejs/kit';
import { createApiClient } from '$lib/server/api';

const PAGE_SIZE = 24;

export const load: PageServerLoad = async ({ url, locals, fetch }) => {
	const query = url.searchParams.get('q')?.trim() ?? '';
	const src = url.searchParams.get('src') === 'yt' ? 'yt' : ('local' as const);
	const page = Math.max(1, Number(url.searchParams.get('page')) || 1);

	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	if (src === 'yt') {
		const emptyResults = { items: [], continuationToken: '' };

		const playlistsPromise = locals.user
			? api.GET('/playlists/users/{userId}', {
					params: { path: { userId: locals.user.sub }, query: { pageNumber: 1, pageSize: 50 } }
				})
			: Promise.resolve(null);

		const searchPromise =
			query && locals.accessToken
				? api.GET('/youtube/search', { params: { query: { query } } })
				: Promise.resolve(null);

		const [playlistsRes, searchRes] = await Promise.all([playlistsPromise, searchPromise]);

		return {
			src: 'yt' as const,
			query,
			ytResults: searchRes?.data ?? emptyResults,
			ytError: Boolean(searchRes?.error),
			playlists: playlistsRes?.data?.items ?? [],
			needsAuth: !locals.user
		};
	}

	const { data, error: err } = await api.GET('/tracks', {
		params: { query: { name: query || undefined, pageNumber: page, pageSize: PAGE_SIZE } }
	});

	if (err || !data) {
		error(502, 'No se pudieron cargar las canciones.');
	}

	return { src: 'local' as const, query, tracks: data };
};

export const actions: Actions = {
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
