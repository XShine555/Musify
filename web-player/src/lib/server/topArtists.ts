export interface HomeArtist {
	id: string;
	name: string;
}

const FILLER_ARTISTS = [
	'Nova Reyes',
	'The Paper Lanterns',
	'Kairo',
	'Lucía Marés',
	'Midnight Transit',
	'Sundown Kids',
	'Aria Volkov',
	'El Bosque Rojo',
	'Halcyon',
	'Marea Baja'
];

export function pickTopArtists(tracks: { artist?: string | null }[], limit: number): HomeArtist[] {
	const seen = new Set<string>();
	const artists: HomeArtist[] = [];

	for (const track of tracks) {
		for (const name of (track.artist ?? '').split(',').map((part) => part.trim())) {
			const key = name.toLowerCase();
			if (!name || seen.has(key)) continue;
			seen.add(key);
			artists.push({ id: name, name });
			if (artists.length >= limit) return artists;
		}
	}

	for (const name of FILLER_ARTISTS) {
		const key = name.toLowerCase();
		if (seen.has(key)) continue;
		seen.add(key);
		artists.push({ id: name, name });
		if (artists.length >= limit) break;
	}

	return artists;
}
