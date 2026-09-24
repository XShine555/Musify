import type { PageServerLoad, Actions } from './$types';
import { apiFor, unwrapOrError } from '$lib/server/api';
import { optionalUser } from '$lib/server/guards';
import { toCount, toPlaylist } from '$lib/server/mappers';
import { PROFILE_PLAYLISTS_PAGE_SIZE } from '$lib/config';
import { followUserAction, unfollowUserAction } from '$lib/server/actions/follow';

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
	const playlists = (playlistsRes.data?.items ?? []).map(toPlaylist);

	const isOwnProfile = viewer !== null && viewer.sub === params.id;

	return {
		profile,
		followersCount: toCount(profile.followersCount),
		playlists,
		isOwnProfile,
		isAnonymous: viewer === null
	};
};

export const actions: Actions = {
	follow: followUserAction,
	unfollow: unfollowUserAction
};
