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

function hash(text: string): number {
	let h = 0;
	for (let i = 0; i < text.length; i++) {
		h = (h * 31 + text.charCodeAt(i)) | 0;
	}
	return Math.abs(h);
}

function pickFillerQueryForUser(userId: string, date: Date): string {
	const dayKey = date.toISOString().slice(0, 10);
	const index = hash(`${userId}:${dayKey}`) % YOUTUBE_FILLER_QUERIES.length;
	return YOUTUBE_FILLER_QUERIES[index];
}

export async function fetchYoutubeFiller(
	api: ReturnType<typeof createApiClient>,
	accessToken: string | null | undefined,
	limit: number,
	seedQuery?: string,
	userId?: string
): Promise<YoutubeFiller> {
	if (!accessToken) return { query: '', items: [], continuationToken: '' };
	const query =
		seedQuery?.trim() ||
		(userId ? pickFillerQueryForUser(userId, new Date()) : pickRandomFillerQuery());
	const { data } = await api.GET('/tracks', { params: { query: { name: query, pageSize: 1 } } });
	const items = (data?.items ?? [])
		.map((item) => item.youTubeSong)
		.filter((song): song is NonNullable<typeof song> => song !== null && song !== undefined);
	return {
		query,
		items: (items as YouTubeSong[]).slice(0, limit),
		continuationToken: data?.nextYoutubeContinuationToken ?? ''
	};
}
