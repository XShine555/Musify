import type { PageServerLoad, Actions } from './$types';
import { createApiClient, playlistCoverTrackIds, requireUser } from '$lib/server/api';
import { fetchYoutubeFiller, shuffle } from '$lib/server/youtube';
import { addTrackAction, addYouTubeToPlaylistAction } from '$lib/server/playlistActions';
import { HOME_LATEST_PAGE_SIZE, YOUTUBE_FILLER_LIMIT } from '$lib/config';
import { pickGreeting } from '$lib/server/greeting';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const latestPromise = api.GET('/tracks', {
		params: { query: { pageNumber: 1, pageSize: HOME_LATEST_PAGE_SIZE } }
	});
	const playlistsPromise = api.GET('/playlists/users/{userId}', {
		params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: 12 } }
	});
	const recentlyPlayedPromise = api.GET('/users/{id}/listening-history', {
		params: { path: { id: user.sub } }
	});

	const [latest, playlists, recentlyPlayed] = await Promise.all([
		latestPromise,
		playlistsPromise,
		recentlyPlayedPromise
	]);
	const playlistItems = playlists.data?.items ?? [];
	const trackIds = await playlistCoverTrackIds(
		api,
		playlistItems.map((p) => p.id)
	);

	return {
		greeting: pickGreeting(new Date().getHours()),
		latest: shuffle((latest.data?.items ?? []).map((track) => ({ kind: 'local' as const, track }))),
		latestHasNext: Boolean(latest.data?.hasNextPage),
		youtubeFiller: fetchYoutubeFiller(api, locals.accessToken, YOUTUBE_FILLER_LIMIT),
		playlists: playlistItems,
		trackIds,
		recentlyPlayed: recentlyPlayed.data ?? []
	};
};

export const actions: Actions = {
	addTrack: addTrackAction,
	addYouTubeToPlaylist: addYouTubeToPlaylistAction
};
