import type { PageServerLoad, Actions } from './$types';
import { createApiClient, unwrapOrError } from '$lib/server/api';
import { addAlbumToPlaylistAction, addTrackAction } from '$lib/server/playlistActions';
import { followUserAction, unfollowUserAction } from '$lib/server/followActions';
import {
	EXPLORE_ALBUMS_PAGE_SIZE,
	EXPLORE_PAGE_SIZE as PAGE_SIZE,
	EXPLORE_USERS_LIMIT
} from '$lib/config';

export const load: PageServerLoad = async ({ url, locals, fetch }) => {
	const query = url.searchParams.get('q')?.trim() ?? '';
	const genreParam = url.searchParams.get('genre')?.trim() || null;
	const page = Math.max(1, Number(url.searchParams.get('page')) || 1);

	const user = locals.user;
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const genresRes = await api.GET('/genres');
	const genres = genresRes.data ?? [];
	const genre = genres.find((item) => item.genre === genreParam)?.genre ?? null;

	const tracksPromise = api.GET('/tracks', {
		params: {
			query: {
				name: query || undefined,
				genre: genre ?? undefined,
				pageNumber: page,
				pageSize: PAGE_SIZE
			}
		}
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

	const usersPromise = query
		? api.GET('/users', {
				params: { query: { usernameSearch: query, pageNumber: 1, pageSize: EXPLORE_USERS_LIMIT } }
			})
		: Promise.resolve(null);

	const [tracksRes, playlistsRes, albumsRes, usersRes] = await Promise.all([
		tracksPromise,
		playlistsPromise,
		albumsPromise,
		usersPromise
	]);

	const tracks = unwrapOrError(tracksRes, 'No se pudieron cargar las canciones.');

	return {
		query,
		genre,
		genres,
		tracks,
		albums: albumsRes?.data?.items ?? [],
		users: usersRes?.data?.items ?? [],
		viewerId: user?.sub ?? null,
		playlists: playlistsRes?.data?.items ?? []
	};
};

export const actions: Actions = {
	addTrack: addTrackAction,
	addAlbumToPlaylist: addAlbumToPlaylistAction,
	follow: followUserAction,
	unfollow: unfollowUserAction
};
