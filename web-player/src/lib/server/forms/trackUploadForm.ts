import { formFile, formString } from '$lib/server/forms/fields';
import { LIMITS } from '$lib/validation';

interface TrackUploadBody {
	title: string;
	audio: File;
	cover: File;
	tags: string[];
	isExplicit: boolean;
}

export function parseTrackUploadForm(
	form: FormData
): { body: TrackUploadBody } | { failMessage: string } {
	const title = formString(form, 'title').trim();
	if (title === '' || title.length > LIMITS.trackTitle) {
		return { failMessage: `El título es obligatorio (máx. ${LIMITS.trackTitle} caracteres).` };
	}

	const audio = formFile(form, 'audio');
	if (!audio) return { failMessage: 'Selecciona un archivo de audio.' };

	const cover = formFile(form, 'cover');
	if (!cover) return { failMessage: 'Selecciona una portada.' };

	const tags = [...new Set(form.getAll('tags').map(String))];
	if (tags.length === 0) return { failMessage: 'Elige al menos un género.' };

	return { body: { title, audio, cover, tags, isExplicit: form.get('isExplicit') === 'on' } };
}
