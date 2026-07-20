import type { Actions, PageServerLoad } from './$types';
import { fail } from '@sveltejs/kit';
import {
	createApiClient,
	requireAccessTokenAction,
	requireUser,
	unwrapOrFail
} from '$lib/server/api';
import { putPresigned, extOf, contentTypeOf, AUDIO_TYPES, IMAGE_TYPES } from '$lib/server/upload';

const MAX_TITLE = 100;

export const load: PageServerLoad = ({ locals, url }) => {
	requireUser(locals, url);
};

export const actions: Actions = {
	default: async ({ request, locals, fetch }) => {
		const accessToken = requireAccessTokenAction(locals, 'Inicia sesión para subir música.');
		if (typeof accessToken !== 'string') return accessToken;

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
		const api = createApiClient({ fetch, accessToken });

		const urlsResult = await api.POST('/tracks/upload-urls', {
			body: {
				pictureFileType: extOf(cover.name) || 'jpg',
				pictureContentType,
				audioFileType: extOf(audio.name) || 'mp3',
				audioContentType,
				expectedPictureSizeBytes: cover.size,
				expectedAudioSizeBytes: audio.size
			}
		});

		const urlsFailure = unwrapOrFail(urlsResult, 'No se pudieron reservar las URLs de subida.');
		if (urlsFailure) return urlsFailure;
		const urls = urlsResult.data!;

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

		const trackResult = await api.POST('/tracks', {
			body: {
				title,
				pictureIntentId: urls.pictureIntentId,
				audioIntentId: urls.audioIntentId
			}
		});

		const trackFailure = unwrapOrFail(trackResult, 'No se pudo crear la pista tras la subida.');
		if (trackFailure) return trackFailure;
		const track = trackResult.data!;

		return { success: true, trackId: track.id, title: track.title };
	}
};
