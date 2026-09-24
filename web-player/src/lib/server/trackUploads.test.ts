import { describe, expect, it, vi } from 'vitest';
import type { ApiClient } from '$lib/server/api';
import {
	UploadError,
	completeTrackUpload,
	parseCompleteRequest,
	parsePresignRequest,
	parseStatusIds,
	presignTrackUploads,
	trackProcessingStatus
} from '$lib/server/trackUploads';

const file = { fileName: 'a.wav', contentType: '', size: 10 };

function api(handlers: {
	get?: (path: string) => unknown;
	post?: (path: string, init: unknown) => unknown;
}) {
	return {
		GET: vi.fn(async (path: string) => handlers.get?.(path) ?? {}),
		POST: vi.fn(async (path: string, init: unknown) => handlers.post?.(path, init) ?? {})
	};
}

const asApi = (fake: unknown) => fake as ApiClient;

describe('parsePresignRequest', () => {
	it('accepts up to the batch size', () => {
		expect(parsePresignRequest({ cover: null, files: [file] }).files).toHaveLength(1);
	});

	it('rejects empty, oversized and malformed batches', () => {
		expect(() => parsePresignRequest({ files: [] })).toThrow(UploadError);
		expect(() => parsePresignRequest({ files: Array(21).fill(file) })).toThrow(UploadError);
		expect(() => parsePresignRequest({ files: [{ fileName: 'a' }] })).toThrow(UploadError);
		expect(() => parsePresignRequest({ files: [{ ...file, size: 0 }] })).toThrow(UploadError);
		expect(() => parsePresignRequest(null)).toThrow(UploadError);
	});

	it('rejects a malformed cover', () => {
		expect(() => parsePresignRequest({ cover: 3, files: [file] })).toThrow(UploadError);
	});
});

describe('presignTrackUploads', () => {
	it('sends every audio and the shared cover, and maps the intents', async () => {
		const fake = api({
			post: () => ({
				data: {
					pictureUploadUrl: 'https://s3/pic',
					pictureContentType: 'image/png',
					audios: [
						{
							intentId: 'i1',
							pictureIntentId: 'p1',
							uploadUrl: 'https://s3/a',
							contentType: 'audio/wav'
						}
					]
				}
			})
		});
		const result = await presignTrackUploads(asApi(fake), {
			cover: { fileName: 'c.png', contentType: 'image/png', size: 5 },
			files: [file]
		});
		expect(result.picture).toEqual({ uploadUrl: 'https://s3/pic', contentType: 'image/png' });
		expect(result.audios[0]).toMatchObject({ intentId: 'i1', pictureIntentId: 'p1' });
		const body = fake.POST.mock.calls[0][1] as { body: Record<string, unknown> };
		expect(body.body).toMatchObject({
			pictureFileType: 'png',
			audios: [{ fileType: 'wav', contentType: 'audio/wav', expectedSizeBytes: 10 }]
		});
	});

	it('has no picture target without a cover', async () => {
		const fake = api({
			post: () => ({
				data: {
					pictureUploadUrl: null,
					pictureContentType: null,
					audios: [{ intentId: 'i1', pictureIntentId: null, uploadUrl: 'u', contentType: 'a' }]
				}
			})
		});
		const result = await presignTrackUploads(asApi(fake), { cover: null, files: [file] });
		expect(result.picture).toBeNull();
	});

	it('fails when the backend errors', async () => {
		const fake = api({ post: () => ({ error: { detail: 'nope' } }) });
		await expect(
			presignTrackUploads(asApi(fake), { cover: null, files: [file] })
		).rejects.toMatchObject({ status: 502, message: 'nope' });
	});
});

describe('parseCompleteRequest', () => {
	const valid = { title: ' Song ', audioIntentId: 'a', tags: ['Rock', 'Rock'] };

	it('trims and dedupes', () => {
		expect(parseCompleteRequest(valid)).toEqual({
			title: 'Song',
			audioIntentId: 'a',
			pictureIntentId: null,
			tags: ['Rock'],
			albumId: null
		});
	});

	it('requires title, intent and genres', () => {
		expect(() => parseCompleteRequest({ ...valid, title: ' ' })).toThrow(UploadError);
		expect(() => parseCompleteRequest({ ...valid, audioIntentId: '' })).toThrow(UploadError);
		expect(() => parseCompleteRequest({ ...valid, tags: [] })).toThrow(UploadError);
	});
});

describe('completeTrackUpload', () => {
	const request = {
		title: 'Song',
		audioIntentId: 'a',
		pictureIntentId: null,
		tags: ['Rock'],
		albumId: null as string | null
	};
	const genres = {
		data: [
			{ genre: 'Rock', incompatibleWith: ['Classical'] },
			{ genre: 'Classical', incompatibleWith: ['Rock'] }
		]
	};

	function build(albumResult: unknown = {}) {
		return api({
			get: () => genres,
			post: (path) => (path === '/tracks' ? { data: { id: 't1' } } : albumResult)
		});
	}

	it('creates an unpublished-by-default track', async () => {
		const fake = build();
		expect(await completeTrackUpload(asApi(fake), request)).toEqual({ trackId: 't1', note: null });
		expect(fake.POST).toHaveBeenCalledTimes(1);
	});

	it('rejects unknown and conflicting genres before creating', async () => {
		const fake = build();
		await expect(
			completeTrackUpload(asApi(fake), { ...request, tags: ['Nope'] })
		).rejects.toMatchObject({ status: 400 });
		await expect(
			completeTrackUpload(asApi(fake), { ...request, tags: ['Rock', 'Classical'] })
		).rejects.toMatchObject({ status: 400 });
		expect(fake.POST).not.toHaveBeenCalled();
	});

	it('adds the track to the album', async () => {
		const fake = build();
		await completeTrackUpload(asApi(fake), { ...request, albumId: 'al' });
		expect(fake.POST).toHaveBeenLastCalledWith('/albums/{id}/tracks', {
			params: { path: { id: 'al' } },
			body: { trackIds: ['t1'] }
		});
	});

	it('keeps the track and reports a note when the album step fails', async () => {
		const fake = build({ error: { status: 500 } });
		const result = await completeTrackUpload(asApi(fake), { ...request, albumId: 'al' });
		expect(result.trackId).toBe('t1');
		expect(result.note).toContain('álbum');
	});
});

describe('track processing status', () => {
	it('parses ids', () => {
		expect(parseStatusIds('a,b,a')).toEqual(['a', 'b']);
		expect(() => parseStatusIds(null)).toThrow(UploadError);
	});

	it('maps the audio status of each track', async () => {
		const fake = api({
			get: (path) => (path === '/tracks/{id}' ? { data: { audioStatus: 'Completed' } } : {})
		});
		expect(await trackProcessingStatus(asApi(fake), ['a'])).toEqual({ a: 'Completed' });
	});
});
