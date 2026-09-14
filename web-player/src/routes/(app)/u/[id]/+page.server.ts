import type { PageServerLoad, Actions } from './$types';
import {
	createApiClient,
	optionalUser,
	requireAccessTokenAction,
	unwrapOrError
} from '$lib/server/api';

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
					params: { path: { playlistId: playlist.id }, query: { pageNumber: 1, pageSize: 1 } }
				})
				.then((res) => Number(res.data?.totalItemCount ?? 0))
		)
	);

	const playlists = playlistItems.map((playlist, i) => ({
		...playlist,
		trackCount: tracksByPlaylist[i]
	}));

	const isOwnProfile = viewer !== null && viewer.sub === params.id;

	const isFollowing =
		viewer && !isOwnProfile
			? await api
					.GET('/users/{id}/is-following', { params: { path: { id: params.id } } })
					.then((res) => res.data ?? false)
					.catch(() => false)
			: false;

	return { profile, playlists, isOwnProfile, isFollowing, isAnonymous: viewer === null };
};

export const actions: Actions = {
	follow: async ({ params, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;

		const api = createApiClient({ fetch, accessToken });
		await api.POST('/users/{id}/follow', { params: { path: { id: params.id } } });
		return { following: true };
	},

	unfollow: async ({ params, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals);
		if (typeof accessToken !== 'string') return accessToken;

		const api = createApiClient({ fetch, accessToken });
		await api.DELETE('/users/{id}/follow', { params: { path: { id: params.id } } });
		return { following: false };
	}
};
