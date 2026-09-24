import { formString } from '$lib/server/api';
import { ALBUM_EARLIEST_YEAR, LIMITS } from '$lib/validation';

interface AlbumFormBody {
	title: string;
	description: string | null;
	releaseYear: number | null;
}

export function parseAlbumForm(form: FormData): { body: AlbumFormBody } | { failMessage: string } {
	const title = formString(form, 'title').trim();
	if (title === '' || title.length > LIMITS.albumTitle) {
		return { failMessage: `El título es obligatorio (máx. ${LIMITS.albumTitle} caracteres).` };
	}

	const description = formString(form, 'description').trim();
	if (description.length > LIMITS.albumDescription) {
		return {
			failMessage: `La descripción no puede pasar de ${LIMITS.albumDescription} caracteres.`
		};
	}

	const rawYear = formString(form, 'releaseYear').trim();
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
