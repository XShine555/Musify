import type { Actions, PageServerLoad } from './$types';
import { fail } from '@sveltejs/kit';
import {
	apiFor,
	authedAction,
	formFile,
	formString,
	requireData,
	requireUser
} from '$lib/server/api';
import { putPresigned, extOf, contentTypeOf, AUDIO_TYPES, IMAGE_TYPES } from '$lib/server/upload';
import { findConflict, genreInfo } from '$lib/data/genres';
import type { components } from '$lib/api/schema';

type Genre = components['schemas']['Genre'];

const MAX_TITLE = 100;

export const load: PageServerLoad = async ({ locals, url, fetch }) => {
	requireUser(locals, url);
	const api = apiFor({ fetch, locals });
	const { data } = await api.GET('/genres/available');
	return { genres: data ?? [] };
};

export const actions: Actions = {
	default: authedAction(async ({ api, form }) => {
		const title = formString(form, 'title').trim();
		const audio = formFile(form, 'audio');
		const cover = formFile(form, 'cover');
		const rawTags = form.getAll('tags').map(String);
		const isExplicit = form.get('isExplicit') === 'on';

		if (title === '' || title.length > MAX_TITLE) {
			return fail(400, { message: 'El título es obligatorio (máx. 100 caracteres).' });
		}
		if (!audio) return fail(400, { message: 'Selecciona un archivo de audio.' });
		if (!cover) return fail(400, { message: 'Selecciona una portada.' });

		if (rawTags.length === 0) {
			return fail(400, { message: 'Elige al menos un género.' });
		}
		const { data: available } = await api.GET('/genres/available');
		const options = available ?? [];
		const known = new Set<string>(options.map((option) => option.genre));
		if (!rawTags.every((tag) => known.has(tag))) {
			return fail(400, { message: 'Alguno de los géneros no es válido.' });
		}
		const tags = [...new Set(rawTags)] as Genre[];
		const conflict = findConflict(tags, options);
		if (conflict) {
			return fail(400, {
				message: `Los géneros ${genreInfo(conflict[0]).label} y ${genreInfo(conflict[1]).label} no se pueden combinar.`
			});
		}

		const audioContentType = contentTypeOf(audio, AUDIO_TYPES);
		const pictureContentType = contentTypeOf(cover, IMAGE_TYPES);

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

		const { data: urls, failure: urlsFailure } = requireData(
			urlsResult,
			'No se pudieron reservar las URLs de subida.'
		);
		if (urlsFailure) return urlsFailure;

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
				audioIntentId: urls.audioIntentId,
				tags,
				isExplicit
			}
		});

		const { data: track, failure: trackFailure } = requireData(
			trackResult,
			'No se pudo crear la pista tras la subida.'
		);
		if (trackFailure) return trackFailure;

		return { success: true, trackId: track.id, title: track.title };
	}, 'Inicia sesión para subir música.')
};
