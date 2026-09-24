import type { PageServerLoad, Actions } from './$types';
import { createApiClient, optionalUser, unwrapOrError } from '$lib/server/api';
import { followUserAction, unfollowUserAction } from '$lib/server/followActions';

const PROFILE_PLAYLISTS_PAGE_SIZE = 50;

export const load: PageServerLoad = async ({ params, locals, url, fetch, parent }) => {
	const { allowAnonymousListening } = await parent();
	const viewer = optionalUser(locals, url, allowAnonymousListening);
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const [profileRes, playlistsRes] = await Promise.all([
		api.GET('/users/{id}/profile', { params: { path: { id: params.id } } }),
		api.GET('/playlists/users/{userId}', {
			params: {
				path: { userId: params.id },
				query: { pageNumber: 1, pageSize: PROFILE_PLAYLISTS_PAGE_SIZE }
			}
		})
	]);

	const profile = unwrapOrError(profileRes, 'Usuario no encontrado.', 404);
	const playlistItems = playlistsRes.data?.items ?? [];

	const tracksByPlaylist = await Promise.all(
		playlistItems.map((playlist) =>
			api
				.GET('/playlists/{id}/tracks', {
					params: { path: { id: playlist.id }, query: { pageNumber: 1, pageSize: 1 } }
				})
				.then((res) => Number(res.data?.totalItemCount ?? 0))
		)
	);

	const playlists = playlistItems.map((playlist, i) => ({
		...playlist,
		trackCount: tracksByPlaylist[i]
	}));

	const isOwnProfile = viewer !== null && viewer.sub === params.id;

	return { profile, playlists, isOwnProfile, isAnonymous: viewer === null };
};

export const actions: Actions = {
	follow: followUserAction,
	unfollow: unfollowUserAction
};
