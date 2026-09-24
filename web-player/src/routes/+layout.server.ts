import { redirect } from '@sveltejs/kit';
import type { LayoutServerLoad } from './$types';
import { getAllowAnonymousListening } from '$lib/server/playbackConfig';
import { createApiClient } from '$lib/server/api';
import { authConfig } from '$lib/server/config';
import { LIKED_TRACKS_PAGE_SIZE, PLAYLIST_PICKER_PAGE_SIZE } from '$lib/config';

const PUBLIC_PATHS = new Set(['/auth']);

async function fetchUserPlaylists(
	fetchFn: typeof fetch,
	accessToken: string | null,
	userId: string
) {
	try {
		const api = createApiClient({ fetch: fetchFn, accessToken: accessToken ?? undefined });
		const { data } = await api.GET('/playlists/users/{userId}', {
			params: {
				path: { userId },
				query: { pageNumber: 1, pageSize: PLAYLIST_PICKER_PAGE_SIZE }
			}
		});
		return { items: data?.items ?? [], total: Number(data?.totalItemCount ?? 0) };
	} catch {
		return { items: [], total: 0 };
	}
}

async function fetchLikedTracks(fetchFn: typeof fetch, accessToken: string | null) {
	try {
		const api = createApiClient({ fetch: fetchFn, accessToken: accessToken ?? undefined });
		const { data } = await api.GET('/likes', {
			params: { query: { pageNumber: 1, pageSize: LIKED_TRACKS_PAGE_SIZE } }
		});
		return data?.items ?? [];
	} catch {
		return [];
	}
}

async function fetchLastPlayedTrack(
	fetchFn: typeof fetch,
	accessToken: string | null,
	userId: string
) {
	try {
		const api = createApiClient({ fetch: fetchFn, accessToken: accessToken ?? undefined });
		const { data } = await api.GET('/users/{id}/last-listened-track', {
			params: { path: { id: userId } }
		});
		return data ?? null;
	} catch {
		return null;
	}
}

export const load: LayoutServerLoad = async ({ locals, url, fetch }) => {
	const allowAnonymousListening = await getAllowAnonymousListening(fetch);

	if (!locals.user && !allowAnonymousListening && !PUBLIC_PATHS.has(url.pathname)) {
		redirect(302, `/auth?returnTo=${encodeURIComponent(url.pathname + url.search)}`);
	}

	const accountUrl = authConfig.issuer
		? `${authConfig.issuer.replace(/\/+$/, '')}/ui/console`
		: null;

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

	const [userPlaylists, likedTracks, lastPlayedTrack] = await Promise.all([
		fetchUserPlaylists(fetch, locals.accessToken, locals.user.sub),
		fetchLikedTracks(fetch, locals.accessToken),
		fetchLastPlayedTrack(fetch, locals.accessToken, locals.user.sub)
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
