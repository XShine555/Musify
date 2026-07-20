import type { createApiClient } from '$lib/server/api';
import type { YouTubeSong } from '$lib/types';

const YOUTUBE_FILLER_QUERIES = [
	'pop',
	'rock',
	'reggaeton',
	'lo-fi',
	'indie',
	'salsa',
	'k-pop',
	'electrónica',
	'jazz',
	'baladas',
	'trap',
	'cumbia',
	'hip hop',
	'música clásica'
];

export interface YoutubeFiller {
	query: string;
	items: YouTubeSong[];
	continuationToken: string;
}

function pickRandomFillerQuery(): string {
	return YOUTUBE_FILLER_QUERIES[Math.floor(Math.random() * YOUTUBE_FILLER_QUERIES.length)];
}

export function shuffle<T>(items: T[]): T[] {
	const copy = [...items];
	for (let i = copy.length - 1; i > 0; i--) {
		const j = Math.floor(Math.random() * (i + 1));
		[copy[i], copy[j]] = [copy[j], copy[i]];
	}
	return copy;
}

export async function fetchYoutubeFiller(
	api: ReturnType<typeof createApiClient>,
	accessToken: string | null | undefined,
	limit: number,
	seed?: string
): Promise<YoutubeFiller> {
	if (!accessToken) return { query: '', items: [], continuationToken: '' };
	const query = seed?.trim() || pickRandomFillerQuery();
	const { data } = await api.GET('/youtube/search', { params: { query: { query } } });
	return {
		query,
		items: ((data?.items ?? []) as YouTubeSong[]).slice(0, limit),
		continuationToken: data?.continuationToken ?? ''
	};
}
