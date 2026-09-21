import { ALBUM_EARLIEST_YEAR } from '$lib/config';

interface AlbumFormBody {
	title: string;
	description: string | null;
	releaseYear: number | null;
}

export function parseAlbumForm(form: FormData): { body: AlbumFormBody } | { failMessage: string } {
	const title = String(form.get('title') ?? '').trim();
	if (title === '' || title.length > 200) {
		return { failMessage: 'El título es obligatorio (máx. 200 caracteres).' };
	}

	const description = String(form.get('description') ?? '').trim();
	if (description.length > 256) {
		return { failMessage: 'La descripción no puede pasar de 256 caracteres.' };
	}

	const rawYear = String(form.get('releaseYear') ?? '').trim();
	const latestYear = new Date().getFullYear() + 1;
	let releaseYear: number | null = null;

	if (rawYear !== '') {
		const parsed = Number(rawYear);
		if (!Number.isInteger(parsed) || parsed < ALBUM_EARLIEST_YEAR || parsed > latestYear) {
			return { failMessage: `El año debe estar entre ${ALBUM_EARLIEST_YEAR} y ${latestYear}.` };
		}
		releaseYear = parsed;
	}

	return {
		body: {
			title,
			description: description === '' ? null : description,
			releaseYear
		}
	};
}
