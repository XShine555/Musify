import type { PageServerLoad, Actions } from './$types';
import { createApiClient, requireUser } from '$lib/server/api';
import { fetchYoutubeFiller } from '$lib/server/youtube';
import {
	addAlbumToPlaylistAction,
	addTrackAction,
	addYouTubeToPlaylistAction
} from '$lib/server/playlistActions';
import {
	HOME_ALBUMS_LIMIT,
	HOME_LATEST_PAGE_SIZE,
	HOME_MIXES_BENTO,
	HOME_SHELF_LIMIT,
	YOUTUBE_FILLER_LIMIT
} from '$lib/config';
import { pickGreeting } from '$lib/server/greeting';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	const user = requireUser(locals, url);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const latestPromise = api.GET('/tracks', {
		params: { query: { pageNumber: 1, pageSize: HOME_LATEST_PAGE_SIZE } }
	});
	const playlistsPromise = api.GET('/playlists/users/{userId}', {
		params: {
			path: { userId: user.sub },
			query: { pageNumber: 1, pageSize: HOME_SHELF_LIMIT + 1 }
		}
	});
	const albumsPromise = api.GET('/albums/recent', {
		params: { query: { limit: HOME_ALBUMS_LIMIT } }
	});
	const mixesPromise = api.GET('/mixes');
	const recentlyPlayedPromise = api.GET('/users/{id}/listening-history', {
		params: { path: { id: user.sub } }
	});

	const [latest, playlists, albums, mixes, recentlyPlayed] = await Promise.all([
		latestPromise,
		playlistsPromise,
		albumsPromise,
		mixesPromise,
		recentlyPlayedPromise
	]);
	const playlistItems = playlists.data?.items ?? [];
	const latestItems = latest.data?.items ?? [];

	return {
		greeting: pickGreeting(new Date().getHours()),
		recentlyPlayed: recentlyPlayed.data ?? [],
		albums: albums.data ?? [],
		mixes: (mixes.data ?? []).slice(0, HOME_MIXES_BENTO),
		playlists: playlistItems.slice(0, HOME_SHELF_LIMIT),
		playlistsHasMore: playlistItems.length > HOME_SHELF_LIMIT,
		newReleases: latestItems,
		youtubeFiller: fetchYoutubeFiller(
			api,
			locals.accessToken,
			YOUTUBE_FILLER_LIMIT,
			undefined,
			user.sub
		)
	};
};

export const actions: Actions = {
	addTrack: addTrackAction,
	addYouTubeToPlaylist: addYouTubeToPlaylistAction,
	addAlbumToPlaylist: addAlbumToPlaylistAction
};
