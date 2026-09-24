import type { LibraryTrack, TrackStatus } from '$lib/types';

export type TrackTone = 'success' | 'neutral' | 'danger';

export const TRACK_STATUS: Record<TrackStatus, { label: string; tone: TrackTone }> = {
	Completed: { label: 'Publicada', tone: 'success' },
	Processing: { label: 'Procesando', tone: 'neutral' },
	Pending: { label: 'En cola', tone: 'neutral' },
	Failed: { label: 'Con error', tone: 'danger' }
};

export const TRACK_FILTERS = [
	{ value: 'all', label: 'Todas', statuses: null },
	{ value: 'published', label: 'Publicadas', statuses: ['Completed'] },
	{ value: 'processing', label: 'Procesando', statuses: ['Pending', 'Processing'] },
	{ value: 'failed', label: 'Con error', statuses: ['Failed'] }
] as const satisfies readonly {
	value: string;
	label: string;
	statuses: readonly TrackStatus[] | null;
}[];

export type TrackFilter = (typeof TRACK_FILTERS)[number]['value'];

export function filterLibraryTracks(
	tracks: LibraryTrack[],
	query: string,
	filter: TrackFilter
): LibraryTrack[] {
	const statuses: readonly TrackStatus[] | null =
		TRACK_FILTERS.find((option) => option.value === filter)?.statuses ?? null;
	const needle = query.trim().toLowerCase();

	return tracks.filter(
		(track) =>
			(!statuses || statuses.includes(track.status)) &&
			(!needle ||
				track.title.toLowerCase().includes(needle) ||
				(track.artist ?? '').toLowerCase().includes(needle))
	);
}
