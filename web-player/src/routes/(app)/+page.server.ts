import { redirect } from '@sveltejs/kit';
import type { PageServerLoad, Actions } from './$types';
import { apiFor } from '$lib/server/api';
import { requireUser } from '$lib/server/guards';
import { toCount, toListeningStats, toMix, toPlaylist, toTrack } from '$lib/server/mappers';
import { addTrackAction } from '$lib/server/actions/playlist';
import { HOME_MIXES_LIMIT, HOME_SHELF_LIMIT, HOME_SPOTLIGHT_TRACKS_LIMIT } from '$lib/config';
import { pickGreeting } from '$lib/server/greeting';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	if (!locals.user) redirect(302, '/explore');
	const user = requireUser(locals, url);
	const api = apiFor({ fetch, locals });

	const playlistsPromise = api.GET('/playlists/users/{userId}', {
		params: {
			path: { userId: user.sub },
			query: { pageNumber: 1, pageSize: HOME_SHELF_LIMIT + 1 }
		}
	});
	const mixesPromise = api.GET('/mixes');
	const recentlyPlayedPromise = api.GET('/users/{id}/listening-history', {
		params: { path: { id: user.sub } }
	});
	const listeningStatsPromise = api
		.GET('/users/{id}/listening-stats', { params: { path: { id: user.sub } } })
		.then((res) => toListeningStats(res.data))
		.catch(() => toListeningStats(null));

	const [playlists, mixes, recentlyPlayed] = await Promise.all([
		playlistsPromise,
		mixesPromise,
		recentlyPlayedPromise
	]);
	const playlistItems = (playlists.data?.items ?? []).map(toPlaylist);

	const spotlightPlaylist = playlistItems[0] ?? null;
	const spotlightTracksPromise = spotlightPlaylist
		? api
				.GET('/playlists/{playlistId}/tracks', {
					params: {
						path: { playlistId: spotlightPlaylist.id },
						query: { pageNumber: 1, pageSize: HOME_SPOTLIGHT_TRACKS_LIMIT }
					}
				})
				.then((res) => ({
					items: (res.data?.items ?? []).map(toTrack),
					totalCount: toCount(res.data?.totalItemCount)
				}))
				.catch(() => ({ items: [], totalCount: 0 }))
		: Promise.resolve({ items: [], totalCount: 0 });

	return {
		greeting: pickGreeting(new Date()),
		recentlyPlayed: (recentlyPlayed.data ?? []).map(toTrack),
		mixes: (mixes.data ?? []).slice(0, HOME_MIXES_LIMIT).map(toMix),
		playlists: playlistItems.slice(0, HOME_SHELF_LIMIT),
		playlistsHasMore: playlistItems.length > HOME_SHELF_LIMIT,
		spotlightPlaylist,
		spotlightTracks: spotlightTracksPromise,
		listeningStats: listeningStatsPromise
	};
};

export const actions: Actions = {
	addTrack: addTrackAction
};
