import type { LayoutServerLoad } from './$types';
import { getAllowAnonymousListening } from '$lib/server/playbackConfig';
import { apiFor, loginRedirect } from '$lib/server/api';
import { toLikedTrack, toPlaylist, toTrack } from '$lib/server/mappers';
import { authConfig } from '$lib/server/config';
import { LIKED_TRACKS_PAGE_SIZE, PLAYLIST_PICKER_PAGE_SIZE } from '$lib/config';

function orFallback<T>(load: Promise<T>, fallback: T): Promise<T> {
	return load.catch(() => fallback);
}

export const load: LayoutServerLoad = async ({ locals, url, fetch }) => {
	const allowAnonymousListening = await getAllowAnonymousListening(fetch);

	if (!locals.user && !allowAnonymousListening) {
		loginRedirect(url);
	}

	const accountUrl = `${authConfig.issuer.replace(/\/+$/, '')}/ui/console`;

	if (!locals.user) {
		return {
			user: locals.user,
			allowAnonymousListening,
			userPlaylists: [],
			userPlaylistsTotal: 0,
			likedTracks: [],
			lastPlayedTrack: null,
			accountUrl
		};
	}

	const api = apiFor({ fetch, locals });
	const userId = locals.user.sub;

	const [userPlaylists, likedTracks, lastPlayedTrack] = await Promise.all([
		orFallback(
			api
				.GET('/playlists/users/{userId}', {
					params: {
						path: { userId },
						query: { pageNumber: 1, pageSize: PLAYLIST_PICKER_PAGE_SIZE }
					}
				})
				.then(({ data }) => ({
					items: (data?.items ?? []).map(toPlaylist),
					total: Number(data?.totalItemCount ?? 0)
				})),
			{ items: [], total: 0 }
		),
		orFallback(
			api
				.GET('/likes', {
					params: { query: { pageNumber: 1, pageSize: LIKED_TRACKS_PAGE_SIZE } }
				})
				.then(({ data }) => (data?.items ?? []).map(toLikedTrack)),
			[]
		),
		orFallback(
			api
				.GET('/users/{id}/last-listened-track', { params: { path: { id: userId } } })
				.then(({ data }) => (data ? toTrack(data) : null)),
			null
		)
	]);

	return {
		user: locals.user,
		allowAnonymousListening,
		userPlaylists: userPlaylists.items,
		userPlaylistsTotal: userPlaylists.total,
		likedTracks,
		lastPlayedTrack,
		accountUrl
	};
};
