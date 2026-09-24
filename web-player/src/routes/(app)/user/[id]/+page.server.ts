import type { PageServerLoad, Actions } from './$types';
import { apiFor, optionalUser, unwrapOrError } from '$lib/server/api';
import { toPlaylistSummary } from '$lib/server/mappers';
import { COUNT_ONLY_PAGE_SIZE, PROFILE_PLAYLISTS_PAGE_SIZE } from '$lib/config';
import { followUserAction, unfollowUserAction } from '$lib/server/followActions';

export const load: PageServerLoad = async ({ params, locals, url, fetch, parent }) => {
	const { allowAnonymousListening } = await parent();
	const viewer = optionalUser(locals, url, allowAnonymousListening);
	const api = apiFor({ fetch, locals });

	const [profileRes, playlistsRes] = await Promise.all([
		api.GET('/users/{id}/profile', { params: { path: { id: params.id } } }),
		api.GET('/playlists/users/{userId}', {
			params: {
				path: { userId: params.id },
				query: { pageNumber: 1, pageSize: PROFILE_PLAYLISTS_PAGE_SIZE, onlyPublic: true }
			}
		})
	]);

	const profile = unwrapOrError(profileRes, 'Usuario no encontrado.', 404);
	const playlistItems = playlistsRes.data?.items ?? [];

	const tracksByPlaylist = await Promise.all(
		playlistItems.map((playlist) =>
			api
				.GET('/playlists/{playlistId}/tracks', {
					params: {
						path: { playlistId: playlist.id },
						query: { pageNumber: 1, pageSize: COUNT_ONLY_PAGE_SIZE }
					}
				})
				.then((res) => Number(res.data?.totalItemCount ?? 0))
		)
	);

	const playlists = playlistItems.map((playlist, i) =>
		toPlaylistSummary(playlist, tracksByPlaylist[i])
	);

	const isOwnProfile = viewer !== null && viewer.sub === params.id;

	return {
		profile,
		followersCount: Number(profile.followersCount),
		playlists,
		isOwnProfile,
		isAnonymous: viewer === null
	};
};

export const actions: Actions = {
	follow: followUserAction,
	unfollow: unfollowUserAction
};
