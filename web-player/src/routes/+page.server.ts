import { redirect } from '@sveltejs/kit';
import type { PageServerLoad, Actions } from './$types';
import { createApiClient, requireUser } from '$lib/server/api';
import { fetchYoutubeAlbumFiller, fetchYoutubeFiller } from '$lib/server/youtube';
import {
	addAlbumToPlaylistAction,
	addTrackAction,
	addYouTubeToPlaylistAction
} from '$lib/server/playlistActions';
import {
	HOME_ALBUM_FILLER_LIMIT,
	HOME_ALBUMS_LIMIT,
	HOME_LATEST_PAGE_SIZE,
	HOME_MIXES_BENTO,
	HOME_POPULAR_FILLER_LIMIT,
	HOME_SHELF_LIMIT,
	HOME_TOP_MUSIC_FILLER_LIMIT,
	YOUTUBE_FILLER_LIMIT
} from '$lib/config';
import { pickGreeting } from '$lib/server/greeting';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	// This dashboard is personal; an anonymous listener goes to /explore instead.
	if (!locals.user) redirect(302, '/explore');
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
	const latestItems = (latest.data?.items ?? []).flatMap((item) =>
		item.track ? [item.track] : []
	);

	return {
		greeting: pickGreeting(new Date().getHours()),
		recentlyPlayed: recentlyPlayed.data ?? [],
		albums: albums.data ?? [],
		mixes: (mixes.data ?? []).slice(0, HOME_MIXES_BENTO),
		playlists: playlistItems.slice(0, HOME_SHELF_LIMIT),
		playlistsHasMore: playlistItems.length > HOME_SHELF_LIMIT,
		newReleases: latestItems,
		topMusicFiller: fetchYoutubeFiller(
			api,
			locals.accessToken,
			HOME_TOP_MUSIC_FILLER_LIMIT,
			undefined,
			user.sub,
			3
		),
		youtubeFiller: fetchYoutubeFiller(
			api,
			locals.accessToken,
			YOUTUBE_FILLER_LIMIT,
			undefined,
			user.sub,
			0
		),
		popularFiller: fetchYoutubeFiller(
			api,
			locals.accessToken,
			HOME_POPULAR_FILLER_LIMIT,
			undefined,
			user.sub,
			1
		),
		albumFiller: fetchYoutubeAlbumFiller(
			api,
			locals.accessToken,
			HOME_ALBUM_FILLER_LIMIT,
			undefined,
			user.sub,
			2
		)
	};
};

export const actions: Actions = {
	addTrack: addTrackAction,
	addYouTubeToPlaylist: addYouTubeToPlaylistAction,
	addAlbumToPlaylist: addAlbumToPlaylistAction
};
