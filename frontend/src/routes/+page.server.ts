import type { PageServerLoad } from './$types';
import { createApiClient } from '$lib/server/api';

export const load: PageServerLoad = async ({ locals, fetch }) => {
	const api = createApiClient({ fetch, accessToken: locals.accessToken ?? undefined });

	const latestPromise = api.GET('/tracks', {
		params: { query: { pageNumber: 1, pageSize: 12 } }
	});
	const playlistsPromise = locals.user
		? api.GET('/playlists/users/{userId}', {
				params: { path: { userId: locals.user.sub }, query: { pageNumber: 1, pageSize: 8 } }
			})
		: Promise.resolve({ data: undefined });

	const [latest, playlists] = await Promise.all([latestPromise, playlistsPromise]);
	const playlistItems = playlists.data?.items ?? [];

	const covers = await Promise.all(
		playlistItems.map(async (playlist) => {
			const { data: tracks } = await api.GET('/playlists/{playlistId}/tracks', {
				params: { path: { playlistId: playlist.id }, query: { pageNumber: 1, pageSize: 4 } }
			});
			return [playlist.id, tracks?.items.map((t) => t.id) ?? []] as const;
		})
	);

	return {
		latest: latest.data?.items ?? [],
		playlists: playlistItems,
		trackIds: Object.fromEntries(covers) as Record<string, string[]>
	};
};
