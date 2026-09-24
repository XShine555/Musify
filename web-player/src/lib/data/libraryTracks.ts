import type { LibraryTrack, TrackStatus, TrackStatusCounts } from '$lib/types';

export type TrackTone = 'success' | 'neutral' | 'danger';

export const TRACK_STATUS: Record<TrackStatus, { label: string; tone: TrackTone }> = {
	Completed: { label: 'Lista', tone: 'neutral' },
	Processing: { label: 'Procesando', tone: 'neutral' },
	Pending: { label: 'En cola', tone: 'neutral' },
	Failed: { label: 'Con error', tone: 'danger' }
};

export function trackState(track: Pick<LibraryTrack, 'status' | 'published'>): {
	label: string;
	tone: TrackTone;
	pulse: boolean;
} {
	if (track.status === 'Completed') {
		return track.published
			? { label: 'Publicada', tone: 'success', pulse: false }
			: { label: 'Sin publicar', tone: 'neutral', pulse: false };
	}
	return {
		...TRACK_STATUS[track.status],
		pulse: track.status === 'Processing' || track.status === 'Pending'
	};
}

export function canPublish(track: Pick<LibraryTrack, 'status'>): boolean {
	return track.status === 'Completed';
}

export interface TrackFilterQuery {
	status?: TrackStatus[];
	published?: boolean;
}

export const TRACK_FILTERS = [
	{ value: 'all', label: 'Todas', query: {} },
	{ value: 'published', label: 'Publicadas', query: { published: true } },
	{
		value: 'unpublished',
		label: 'Sin publicar',
		query: { status: ['Completed'], published: false }
	},
	{ value: 'processing', label: 'Procesando', query: { status: ['Pending', 'Processing'] } },
	{ value: 'failed', label: 'Con error', query: { status: ['Failed'] } }
] as const satisfies readonly { value: string; label: string; query: TrackFilterQuery }[];

export type TrackFilter = (typeof TRACK_FILTERS)[number]['value'];

export function parseTrackFilter(value: string | null): TrackFilter {
	return TRACK_FILTERS.find((option) => option.value === value)?.value ?? 'all';
}

export function filterQuery(filter: TrackFilter): TrackFilterQuery {
	return TRACK_FILTERS.find((option) => option.value === filter)?.query ?? {};
}

export function filterCounts(counts: TrackStatusCounts): Record<TrackFilter, number> {
	return {
		all: counts.total,
		published: counts.published,
		unpublished: Math.max(0, counts.completed - counts.published),
		processing: counts.processing,
		failed: counts.failed
	};
}
