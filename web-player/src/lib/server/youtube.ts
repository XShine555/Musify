import type { createApiClient } from '$lib/server/api';
import type { YouTubeAlbumResult, YouTubeSong } from '$lib/types';

export const YOUTUBE_FILLER_QUERIES = [
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

export interface YoutubeAlbumFiller {
	query: string;
	items: YouTubeAlbumResult[];
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

function pickFillerQueryForUser(userId: string, date: Date, variant = 0): string {
	const dayKey = date.toISOString().slice(0, 10);
	const index = (hash(`${userId}:${dayKey}`) + variant) % YOUTUBE_FILLER_QUERIES.length;
	return YOUTUBE_FILLER_QUERIES[index];
}

export async function fetchYoutubeFiller(
	api: ReturnType<typeof createApiClient>,
	accessToken: string | null | undefined,
	limit: number,
	seedQuery?: string,
	userId?: string,
	variant = 0
): Promise<YoutubeFiller> {
	if (!accessToken) return { query: '', items: [], continuationToken: '' };
	const query =
		seedQuery?.trim() ||
		(userId ? pickFillerQueryForUser(userId, new Date(), variant) : pickRandomFillerQuery());
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

export async function fetchYoutubeAlbumFiller(
	api: ReturnType<typeof createApiClient>,
	accessToken: string | null | undefined,
	limit: number,
	seedQuery?: string,
	userId?: string,
	variant = 0
): Promise<YoutubeAlbumFiller> {
	if (!accessToken) return { query: '', items: [] };
	const query =
		seedQuery?.trim() ||
		(userId ? pickFillerQueryForUser(userId, new Date(), variant) : pickRandomFillerQuery());
	const { data } = await api.GET('/albums', {
		params: { query: { title: query, pageNumber: 1, pageSize: limit + 5 } }
	});
	const items = (data?.items ?? [])
		.map((item) => item.youTubeAlbum)
		.filter((album): album is NonNullable<typeof album> => album !== null && album !== undefined);
	return { query, items: (items as YouTubeAlbumResult[]).slice(0, limit) };
}
