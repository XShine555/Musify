import type { Actions } from './$types';
import { fail } from '@sveltejs/kit';
import { createApiClient } from '$lib/server/api';
import { putPresigned } from '$lib/server/upload';

const MAX_TITLE = 100;

const AUDIO_TYPES: Record<string, string> = {
	mp3: 'audio/mpeg',
	m4a: 'audio/mp4',
	aac: 'audio/aac',
	flac: 'audio/flac',
	wav: 'audio/wav',
	ogg: 'audio/ogg',
	opus: 'audio/opus'
};

const IMAGE_TYPES: Record<string, string> = {
	jpg: 'image/jpeg',
	jpeg: 'image/jpeg',
	png: 'image/png',
	webp: 'image/webp'
};

function extOf(name: string): string {
	const dot = name.lastIndexOf('.');
	return dot >= 0 ? name.slice(dot + 1).toLowerCase() : '';
}

function contentTypeOf(file: File, table: Record<string, string>): string {
	if (file.type) return file.type;
	return table[extOf(file.name)] ?? 'application/octet-stream';
}

export const actions: Actions = {
	default: async ({ request, locals, fetch }) => {
		if (!locals.accessToken) {
			return fail(401, { message: 'Inicia sesión para subir música.' });
		}

		const form = await request.formData();
		const title = String(form.get('title') ?? '').trim();
		const audio = form.get('audio');
		const cover = form.get('cover');

		if (title === '' || title.length > MAX_TITLE) {
			return fail(400, { message: 'El título es obligatorio (máx. 100 caracteres).' });
		}
		if (!(audio instanceof File) || audio.size === 0) {
			return fail(400, { message: 'Selecciona un archivo de audio.' });
		}
		if (!(cover instanceof File) || cover.size === 0) {
			return fail(400, { message: 'Selecciona una portada.' });
		}

		const audioContentType = contentTypeOf(audio, AUDIO_TYPES);
		const pictureContentType = contentTypeOf(cover, IMAGE_TYPES);
		const api = createApiClient({ fetch, accessToken: locals.accessToken });

		const { data: urls, error: urlsError } = await api.POST('/tracks/upload-urls', {
			body: {
				pictureFileType: extOf(cover.name) || 'jpg',
				pictureContentType,
				audioFileType: extOf(audio.name) || 'mp3',
				audioContentType,
				expectedPictureSizeBytes: cover.size,
				expectedAudioSizeBytes: audio.size
			}
		});

		if (urlsError || !urls) {
			return fail(502, { message: 'No se pudieron reservar las URLs de subida.' });
		}

		try {
			const [audioBuffer, coverBuffer] = await Promise.all([
				audio.arrayBuffer(),
				cover.arrayBuffer()
			]);
			await Promise.all([
				putPresigned(urls.audioUploadUrl, Buffer.from(audioBuffer), urls.audioContentType),
				putPresigned(urls.pictureUploadUrl, Buffer.from(coverBuffer), urls.pictureContentType)
			]);
		} catch (err) {
			return fail(502, {
				message: 'Falló la subida del archivo al almacenamiento.',
				detail: err instanceof Error ? err.message : undefined
			});
		}

		const { data: track, error: createError } = await api.POST('/tracks', {
			body: {
				title,
				pictureIntentId: urls.pictureIntentId,
				audioIntentId: urls.audioIntentId
			}
		});

		if (createError || !track) {
			return fail(502, { message: 'No se pudo crear la pista tras la subida.' });
		}

		return { success: true, trackId: track.id, title: track.title };
	}
};
