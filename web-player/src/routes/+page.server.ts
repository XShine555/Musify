import { redirect } from '@sveltejs/kit';
import type { PageServerLoad, Actions } from './$types';
import { createApiClient, requireUser } from '$lib/server/api';
import { addTrackAction } from '$lib/server/playlistActions';
import {
	HOME_LATEST_PAGE_SIZE,
	HOME_MIXES_LIMIT,
	HOME_SHELF_LIMIT,
	HOME_SPOTLIGHT_TRACKS_LIMIT
} from '$lib/config';
import { pickGreeting } from '$lib/server/greeting';

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
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
	const mixesPromise = api.GET('/mixes');
	const recentlyPlayedPromise = api.GET('/users/{id}/listening-history', {
		params: { path: { id: user.sub } }
	});
	const listeningStatsPromise = api
		.GET('/users/{id}/listening-stats', { params: { path: { id: user.sub } } })
		.then((res) => res.data ?? { tracksThisWeek: 0, secondsThisWeek: 0, streakDays: 0 })
		.catch(() => ({ tracksThisWeek: 0, secondsThisWeek: 0, streakDays: 0 }));

	const [latest, playlists, mixes, recentlyPlayed] = await Promise.all([
		latestPromise,
		playlistsPromise,
		mixesPromise,
		recentlyPlayedPromise
	]);
	const playlistItems = playlists.data?.items ?? [];
	const latestItems = latest.data?.items ?? [];

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
					items: res.data?.items ?? [],
					totalCount: Number(res.data?.totalItemCount ?? 0)
				}))
				.catch(() => ({ items: [], totalCount: 0 }))
		: Promise.resolve({ items: [], totalCount: 0 });

	return {
		greeting: pickGreeting(new Date()),
		recentlyPlayed: recentlyPlayed.data ?? [],
		mixes: (mixes.data ?? []).slice(0, HOME_MIXES_LIMIT),
		playlists: playlistItems.slice(0, HOME_SHELF_LIMIT),
		playlistsHasMore: playlistItems.length > HOME_SHELF_LIMIT,
		newReleases: latestItems,
		spotlightPlaylist,
		spotlightTracks: spotlightTracksPromise,
		listeningStats: listeningStatsPromise
	};
};

export const actions: Actions = {
	addTrack: addTrackAction
};
