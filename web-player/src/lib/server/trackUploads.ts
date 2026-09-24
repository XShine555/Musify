import type { ApiClient } from '$lib/server/api';
import { apiErrorDetail } from '$lib/server/api';
import { AUDIO_TYPES, IMAGE_TYPES, extOf } from '$lib/server/upload';
import { genreConflictMessage, UPLOAD_BATCH_SIZE, UPLOAD_MAX_BYTES } from '$lib/data/uploadQueue';
import type {
	CompleteRequest,
	CompleteResponse,
	PresignFile,
	PresignRequest,
	PresignResponse,
	StatusResponse,
	TrackProcessing
} from '$lib/data/uploadProtocol';
import { LIMITS } from '$lib/validation';
import type { components } from '$lib/api/schema';

type Genre = components['schemas']['Genre'];

export class UploadError extends Error {
	constructor(
		message: string,
		readonly status: number
	) {
		super(message);
	}
}

function contentType(file: PresignFile, table: Record<string, string>): string {
	return file.contentType || table[extOf(file.fileName)] || 'application/octet-stream';
}

function isFile(value: unknown): value is PresignFile {
	if (typeof value !== 'object' || value === null) return false;
	const { fileName, contentType: type, size } = value as Record<string, unknown>;
	return (
		typeof fileName === 'string' &&
		fileName !== '' &&
		typeof type === 'string' &&
		typeof size === 'number' &&
		Number.isFinite(size)
	);
}

export function parsePresignRequest(payload: unknown): PresignRequest {
	const { cover, files } = (payload ?? {}) as { cover?: unknown; files?: unknown };
	if (!Array.isArray(files) || files.length === 0 || files.length > UPLOAD_BATCH_SIZE) {
		throw new UploadError(`Envía entre 1 y ${UPLOAD_BATCH_SIZE} archivos.`, 400);
	}
	if (!files.every(isFile)) throw new UploadError('Archivo de audio inválido.', 400);
	if (files.some((file) => file.size <= 0 || file.size > UPLOAD_MAX_BYTES)) {
		throw new UploadError('Un archivo está vacío o supera el tamaño máximo.', 400);
	}
	if (cover !== null && cover !== undefined && !isFile(cover)) {
		throw new UploadError('Portada inválida.', 400);
	}
	return { cover: cover ?? null, files };
}

export async function presignTrackUploads(
	api: ApiClient,
	request: PresignRequest
): Promise<PresignResponse> {
	const { cover, files } = request;
	const result = await api.POST('/tracks/upload-urls', {
		body: {
			pictureFileType: cover ? extOf(cover.fileName) || 'jpg' : null,
			pictureContentType: cover ? contentType(cover, IMAGE_TYPES) : null,
			expectedPictureSizeBytes: cover?.size ?? null,
			audios: files.map((file) => ({
				fileType: extOf(file.fileName) || 'mp3',
				contentType: contentType(file, AUDIO_TYPES),
				expectedSizeBytes: file.size
			}))
		}
	});

	if (result.error || !result.data || result.data.audios.length !== files.length) {
		throw new UploadError(
			apiErrorDetail(result.error) ?? 'No se pudieron reservar las URLs de subida.',
			502
		);
	}

	const { pictureUploadUrl, pictureContentType, audios } = result.data;
	return {
		picture:
			pictureUploadUrl && pictureContentType
				? { uploadUrl: pictureUploadUrl, contentType: pictureContentType }
				: null,
		audios: audios.map((audio) => ({
			intentId: audio.intentId,
			pictureIntentId: audio.pictureIntentId,
			uploadUrl: audio.uploadUrl,
			contentType: audio.contentType
		}))
	};
}

function idOrNull(value: unknown): string | null {
	return typeof value === 'string' && value !== '' ? value : null;
}

export function parseCompleteRequest(payload: unknown): CompleteRequest {
	const body = (payload ?? {}) as Record<string, unknown>;
	const title = typeof body.title === 'string' ? body.title.trim() : '';
	if (title === '' || title.length > LIMITS.trackTitle) {
		throw new UploadError(`El título es obligatorio (máx. ${LIMITS.trackTitle} caracteres).`, 400);
	}
	const audioIntentId = idOrNull(body.audioIntentId);
	if (!audioIntentId) throw new UploadError('Subida inválida.', 400);
	const tags = Array.isArray(body.tags)
		? [...new Set(body.tags.filter((tag): tag is string => typeof tag === 'string'))]
		: [];
	if (tags.length === 0) throw new UploadError('Elige al menos un género.', 400);
	return {
		title,
		audioIntentId,
		pictureIntentId: idOrNull(body.pictureIntentId),
		tags,
		albumId: idOrNull(body.albumId)
	};
}

export async function completeTrackUpload(
	api: ApiClient,
	request: CompleteRequest
): Promise<CompleteResponse> {
	const { data: available } = await api.GET('/genres/available');
	const options = available ?? [];
	const known = new Set<string>(options.map((option) => option.genre));
	if (!request.tags.every((tag) => known.has(tag))) {
		throw new UploadError('Alguno de los géneros no es válido.', 400);
	}
	const conflict = genreConflictMessage(request.tags, options);
	if (conflict) throw new UploadError(conflict, 400);

	const created = await api.POST('/tracks', {
		body: {
			title: request.title,
			pictureIntentId: request.pictureIntentId,
			audioIntentId: request.audioIntentId,
			tags: request.tags as Genre[],
			isExplicit: false
		}
	});
	if (created.error || !created.data) {
		throw new UploadError(
			apiErrorDetail(created.error) ?? 'No se pudo crear la canción tras la subida.',
			502
		);
	}
	const trackId = created.data.id;

	if (!request.albumId) return { trackId, note: null };
	const added = await api.POST('/albums/{id}/tracks', {
		params: { path: { id: request.albumId } },
		body: { trackIds: [trackId] }
	});
	return {
		trackId,
		note: added.error ? 'Se creó, pero no se pudo añadir al álbum.' : null
	};
}

export function parseStatusIds(raw: string | null): string[] {
	const ids = [...new Set((raw ?? '').split(',').filter(Boolean))];
	if (ids.length === 0 || ids.length > UPLOAD_BATCH_SIZE) {
		throw new UploadError('Canciones inválidas.', 400);
	}
	return ids;
}

export async function trackProcessingStatus(
	api: ApiClient,
	ids: string[]
): Promise<StatusResponse> {
	const entries = await Promise.all(
		ids.map(async (id) => {
			const result = await api.GET('/tracks/{id}', { params: { path: { id } } });
			return [id, result.data?.audioStatus] as const;
		})
	);
	const known = entries.filter(
		(entry): entry is readonly [string, TrackProcessing] => entry[1] !== undefined
	);
	return Object.fromEntries(known);
}
