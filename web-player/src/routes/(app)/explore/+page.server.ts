import type { PageServerLoad, Actions } from './$types';
import { createApiClient, unwrapOrError } from '$lib/server/api';
import { fetchYoutubeFiller } from '$lib/server/youtube';
import {
	addAlbumToPlaylistAction,
	addTrackAction,
	addYouTubeToPlaylistAction,
	addYoutubeAlbumToPlaylistAction
} from '$lib/server/playlistActions';
import {
	EXPLORE_ALBUMS_PAGE_SIZE,
	EXPLORE_PAGE_SIZE as PAGE_SIZE,
	YOUTUBE_FILLER_LIMIT
} from '$lib/config';
import { searchUsers } from '$lib/server/userDirectory';

export const load: PageServerLoad = async ({ url, locals, fetch }) => {
	const query = url.searchParams.get('q')?.trim() ?? '';
	const page = Math.max(1, Number(url.searchParams.get('page')) || 1);

	// This page is reachable without a session when anonymous listening is on
	// (see +layout.server.ts), so `user` may be null here.
	const user = locals.user;
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const tracksPromise = api.GET('/tracks', {
		params: { query: { name: query || undefined, pageNumber: page, pageSize: PAGE_SIZE } }
	});

	const playlistsPromise = user
		? api.GET('/playlists/users/{userId}', {
				params: { path: { userId: user.sub }, query: { pageNumber: 1, pageSize: 50 } }
			})
		: Promise.resolve(null);

	const albumsPromise = query
		? api.GET('/albums', {
				params: { query: { title: query, pageNumber: 1, pageSize: EXPLORE_ALBUMS_PAGE_SIZE } }
			})
		: Promise.resolve(null);

	const [tracksRes, playlistsRes, albumsRes] = await Promise.all([
		tracksPromise,
		playlistsPromise,
		albumsPromise
	]);

	const tracks = unwrapOrError(tracksRes, 'No se pudieron cargar las canciones.');

	const albumItems = albumsRes?.data?.items ?? [];

	return {
		query,
		tracks,
		albums: albumItems.flatMap((item) => (item.album ? [item.album] : [])),
		youtubeAlbums: albumItems.flatMap((item) => (item.youTubeAlbum ? [item.youTubeAlbum] : [])),
		users: searchUsers(query),
		youtubeFiller: query
			? Promise.resolve(null)
			: fetchYoutubeFiller(api, locals.accessToken, YOUTUBE_FILLER_LIMIT),
		playlists: playlistsRes?.data?.items ?? []
	};
};

export const actions: Actions = {
	addTrack: addTrackAction,
	addYouTubeToPlaylist: addYouTubeToPlaylistAction,
	addAlbumToPlaylist: addAlbumToPlaylistAction,
	addYoutubeAlbumToPlaylist: addYoutubeAlbumToPlaylistAction
};
