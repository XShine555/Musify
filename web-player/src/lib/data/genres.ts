export const GENRE_INFO: Record<string, { label: string; tagline: string }> = {
	pop: { label: 'Pop', tagline: 'Éxitos que suenan en todas partes' },
	rock: { label: 'Rock', tagline: 'Guitarras, actitud y ruido' },
	reggaeton: { label: 'Reggaetón', tagline: 'El ritmo que no para' },
	'lo-fi': { label: 'Lo-fi', tagline: 'Para concentrarte o relajarte' },
	indie: { label: 'Indie', tagline: 'Voces fuera del radar' },
	salsa: { label: 'Salsa', tagline: 'Para mover el cuerpo' },
	'k-pop': { label: 'K-pop', tagline: 'Coreografías pegajosas' },
	electrónica: { label: 'Electrónica', tagline: 'Beats para perderte' },
	jazz: { label: 'Jazz', tagline: 'Improvisación y elegancia' },
	baladas: { label: 'Baladas', tagline: 'Para sentir con calma' },
	trap: { label: 'Trap', tagline: 'Autotune y bajos pesados' },
	cumbia: { label: 'Cumbia', tagline: 'El sabor de siempre' },
	'hip hop': { label: 'Hip hop', tagline: 'Rimas con actitud' },
	'música clásica': { label: 'Clásica', tagline: 'Siglos de composición' }
};

export const genreTiles = Object.entries(GENRE_INFO).map(([query, info], i, all) => ({
	query,
	hue: Math.round((i * 360) / all.length),
	...info
}));
