import { createApiClient } from '$lib/server/api';

const CACHE_TTL_MS = 30_000;

let cached: { allowAnonymousListening: boolean; expiresAt: number } | null = null;

export async function getAllowAnonymousListening(fetchFn: typeof fetch): Promise<boolean> {
	if (cached && cached.expiresAt > Date.now()) return cached.allowAnonymousListening;

	try {
		const api = createApiClient({ fetch: fetchFn });
		const { data } = await api.GET('/config/playback');
		const allowAnonymousListening = data?.allowAnonymousListening ?? false;

		cached = { allowAnonymousListening, expiresAt: Date.now() + CACHE_TTL_MS };
		return allowAnonymousListening;
	} catch {
		return false;
	}
}
