import { formFile, formString } from '$lib/server/api';
import { LIMITS } from '$lib/validation';

interface PlaylistFormBody {
	name: string;
	description: string;
	visibility: 'Public' | 'Private';
	cover: File | null;
}

export type PlaylistFormFailure = { failMessage: string; name: string; description: string };

export function parsePlaylistForm(
	form: FormData
): { body: PlaylistFormBody } | PlaylistFormFailure {
	const name = formString(form, 'name').trim();
	const description = formString(form, 'description').trim();

	if (name === '' || name.length > LIMITS.playlistName) {
		return {
			failMessage: `El nombre es obligatorio (máx. ${LIMITS.playlistName} caracteres).`,
			name,
			description
		};
	}
	if (description.length > LIMITS.playlistDescription) {
		return {
			failMessage: `La descripción no puede pasar de ${LIMITS.playlistDescription} caracteres.`,
			name,
			description
		};
	}

	return {
		body: {
			name,
			description,
			visibility: formString(form, 'visibility') === 'public' ? 'Public' : 'Private',
			cover: formFile(form, 'cover')
		}
	};
}
