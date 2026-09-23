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
	const page = Math.max(1, Number(url.searchParams.get('page')) || 1);

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

	const userItems = usersRes?.data?.items ?? [];
	const followedIds = user
		? await Promise.all(
				userItems
					.filter((u) => u.id !== user.sub)
					.map((u) =>
						api
							.GET('/users/{id}/is-following', { params: { path: { id: u.id } } })
							.then((res) => (res.data ? u.id : null))
							.catch(() => null)
					)
			)
		: [];
	const followed = new Set(followedIds);

	return {
		query,
		tracks,
		albums: (albumsRes?.data?.items ?? []).map((item) => item.album),
		users: userItems.map((u) => ({
			...u,
			isSelf: user?.sub === u.id,
			isFollowing: followed.has(u.id)
		})),
		canFollow: !!user,
		playlists: playlistsRes?.data?.items ?? []
	};
};

export const actions: Actions = {
	addTrack: addTrackAction,
	addAlbumToPlaylist: addAlbumToPlaylistAction,
	follow: followUserAction,
	unfollow: unfollowUserAction
};
