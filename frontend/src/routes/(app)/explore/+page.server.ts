import type { PageServerLoad, Actions } from './$types';
import { createApiClient, unwrapOrError } from '$lib/server/api';
import { fetchYoutubeFiller } from '$lib/server/youtube';
import { addTrackAction, addYouTubeToPlaylistAction } from '$lib/server/playlistActions';
import { EXPLORE_PAGE_SIZE as PAGE_SIZE, YOUTUBE_FILLER_LIMIT } from '$lib/config';

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

	const tracks = unwrapOrError(tracksRes, 'No se pudieron cargar las canciones.');

	return {
		query,
		tracks,
		ytResults: searchRes?.data ?? null,
		ytError: Boolean(searchRes?.error),
		youtubeFiller: query
			? Promise.resolve(null)
			: fetchYoutubeFiller(api, locals.accessToken, YOUTUBE_FILLER_LIMIT),
		playlists: playlistsRes?.data?.items ?? [],
		needsAuth: !locals.user
	};
};

export const actions: Actions = {
	addTrack: addTrackAction,
	addYouTubeToPlaylist: addYouTubeToPlaylistAction
};
