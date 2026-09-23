const GENRE_INFO: Record<string, { label: string; tagline: string }> = {
	Pop: { label: 'Pop', tagline: 'Éxitos que suenan en todas partes' },
	Rock: { label: 'Rock', tagline: 'Guitarras, actitud y ruido' },
	HipHop: { label: 'Hip hop', tagline: 'Rimas con actitud' },
	RnB: { label: 'R&B', tagline: 'Ritmo suave y voces con alma' },
	Jazz: { label: 'Jazz', tagline: 'Improvisación y elegancia' },
	Blues: { label: 'Blues', tagline: 'Sentimiento en cada nota' },
	Classical: { label: 'Clásica', tagline: 'Siglos de composición' },
	Electronic: { label: 'Electrónica', tagline: 'Beats para perderte' },
	House: { label: 'House', tagline: 'Pista de baile sin fin' },
	Techno: { label: 'Techno', tagline: 'Pulso hipnótico' },
	Trance: { label: 'Trance', tagline: 'Melodías que te elevan' },
	Dubstep: { label: 'Dubstep', tagline: 'Bajos que hacen temblar' },
	DrumAndBass: { label: 'Drum and bass', tagline: 'Velocidad y graves' },
	Metal: { label: 'Metal', tagline: 'Riffs pesados' },
	Punk: { label: 'Punk', tagline: 'Rápido, crudo y directo' },
	Reggae: { label: 'Reggae', tagline: 'Buenas vibraciones' },
	Reggaeton: { label: 'Reggaetón', tagline: 'El ritmo que no para' },
	Country: { label: 'Country', tagline: 'Historias con guitarra' },
	Folk: { label: 'Folk', tagline: 'Raíces y acústico' },
	Indie: { label: 'Indie', tagline: 'Voces fuera del radar' },
	KPop: { label: 'K-pop', tagline: 'Coreografías pegajosas' },
	Latin: { label: 'Latina', tagline: 'Sabor y ritmo' },
	Soul: { label: 'Soul', tagline: 'Emoción en estado puro' },
	Funk: { label: 'Funk', tagline: 'Groove imposible de quedarse quieto' },
	Ambient: { label: 'Ambient', tagline: 'Paisajes sonoros' },
	Lofi: { label: 'Lo-fi', tagline: 'Para concentrarte o relajarte' }
};

const KNOWN_GENRES = Object.keys(GENRE_INFO);

function humanize(name: string): string {
	return name.replace(/([a-z])([A-Z])/g, '$1 $2');
}

export function genreInfo(genre: string) {
	return GENRE_INFO[genre] ?? { label: humanize(genre), tagline: '' };
}

export function genreHue(genre: string): number {
	const known = KNOWN_GENRES.indexOf(genre);
	if (known >= 0) return Math.round((known * 360) / KNOWN_GENRES.length);
	let hash = 0;
	for (const char of genre) hash = (hash * 31 + char.charCodeAt(0)) % 360;
	return hash;
}

export function genreTiles(genres: { genre: string }[]) {
	return genres.map(({ genre }) => ({
		genre,
		hue: genreHue(genre),
		...genreInfo(genre)
	}));
}

export function genreOptions(genres: { genre: string; incompatibleWith: string[] }[]) {
	return genres.map(({ genre, incompatibleWith }) => ({
		genre,
		incompatibleWith,
		label: genreInfo(genre).label
	}));
}

export function findConflict(
	tags: string[],
	options: { genre: string; incompatibleWith: string[] }[]
): [string, string] | undefined {
	const byGenre = new Map(options.map((option) => [option.genre, option.incompatibleWith]));
	for (let i = 0; i < tags.length; i++) {
		for (let j = i + 1; j < tags.length; j++) {
			if (byGenre.get(tags[i])?.includes(tags[j])) return [tags[i], tags[j]];
		}
	}
	return undefined;
}
