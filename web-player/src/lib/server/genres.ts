import type { ApiClient } from '$lib/server/api';
import type { components } from '$lib/api/schema';

type GenreInfo = components['schemas']['GenreResponse'];

const CACHE_TTL_MS = 5 * 60_000;

let cached: { genres: GenreInfo[]; expiresAt: number } | null = null;

export async function getGenres(api: ApiClient): Promise<GenreInfo[]> {
	if (cached && cached.expiresAt > Date.now()) return cached.genres;

	const { data } = await api.GET('/genres');
	if (!data) return cached?.genres ?? [];

	cached = { genres: data, expiresAt: Date.now() + CACHE_TTL_MS };
	return data;
}
