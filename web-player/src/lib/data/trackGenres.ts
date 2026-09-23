export const TRACK_GENRES = [
	{ value: 'Pop', label: 'Pop' },
	{ value: 'Rock', label: 'Rock' },
	{ value: 'HipHop', label: 'Hip-hop' },
	{ value: 'RnB', label: 'R&B' },
	{ value: 'Jazz', label: 'Jazz' },
	{ value: 'Blues', label: 'Blues' },
	{ value: 'Classical', label: 'Clásica' },
	{ value: 'Electronic', label: 'Electrónica' },
	{ value: 'House', label: 'House' },
	{ value: 'Techno', label: 'Techno' },
	{ value: 'Trance', label: 'Trance' },
	{ value: 'Dubstep', label: 'Dubstep' },
	{ value: 'DrumAndBass', label: 'Drum and bass' },
	{ value: 'Metal', label: 'Metal' },
	{ value: 'Punk', label: 'Punk' },
	{ value: 'Reggae', label: 'Reggae' },
	{ value: 'Reggaeton', label: 'Reggaetón' },
	{ value: 'Country', label: 'Country' },
	{ value: 'Folk', label: 'Folk' },
	{ value: 'Indie', label: 'Indie' },
	{ value: 'KPop', label: 'K-pop' },
	{ value: 'Latin', label: 'Latina' },
	{ value: 'Soul', label: 'Soul' },
	{ value: 'Funk', label: 'Funk' },
	{ value: 'Ambient', label: 'Ambient' },
	{ value: 'Lofi', label: 'Lo-fi' }
] as const;

export type TrackGenre = (typeof TRACK_GENRES)[number]['value'];

const GENRE_VALUES: ReadonlySet<string> = new Set(TRACK_GENRES.map((genre) => genre.value));

const INTENSE: ReadonlySet<TrackGenre> = new Set(['Metal', 'Punk', 'Dubstep', 'DrumAndBass']);
const MELLOW: ReadonlySet<TrackGenre> = new Set(['Classical', 'Ambient', 'Lofi']);

export function isTrackGenre(value: string): value is TrackGenre {
	return GENRE_VALUES.has(value);
}

export function areCompatible(a: TrackGenre, b: TrackGenre): boolean {
	return !((INTENSE.has(a) && MELLOW.has(b)) || (INTENSE.has(b) && MELLOW.has(a)));
}

export function genreLabel(value: TrackGenre): string {
	return TRACK_GENRES.find((genre) => genre.value === value)?.label ?? value;
}

export function findConflict(tags: readonly TrackGenre[]): [TrackGenre, TrackGenre] | undefined {
	for (let i = 0; i < tags.length; i++) {
		for (let j = i + 1; j < tags.length; j++) {
			if (!areCompatible(tags[i], tags[j])) return [tags[i], tags[j]];
		}
	}
	return undefined;
}
