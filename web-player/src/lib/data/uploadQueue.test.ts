import { describe, expect, it } from 'vitest';
import {
	beginUpload,
	chunkStartable,
	failUpload,
	finishCreate,
	finishProcessing,
	finishUpload,
	genreConflictMessage,
	hasActiveUploads,
	reportProgress,
	retryUpload,
	summarizeQueue,
	titleFromFileName,
	toQueuedUpload,
	uploadError,
	uploadLimitsLabel,
	UPLOAD_MAX_BYTES
} from '$lib/data/uploadQueue';

function file(name: string, size = 1024) {
	return new File([new Uint8Array(size)], name);
}

describe('titleFromFileName', () => {
	it('drops the extension and track number and humanizes underscores', () => {
		expect(titleFromFileName('05_ultima_ola_master.wav')).toBe('Ultima ola master');
		expect(titleFromFileName('12 - Niebla.flac')).toBe('Niebla');
	});

	it('keeps the base name when nothing is left after cleaning', () => {
		expect(titleFromFileName('07.mp3')).toBe('07');
	});
});

describe('uploadError', () => {
	it('accepts supported formats', () => {
		for (const name of ['a.wav', 'a.FLAC', 'a.mp3', 'a.aiff']) {
			expect(uploadError(file(name))).toBeNull();
		}
	});

	it('rejects unsupported formats, empty and oversized files', () => {
		expect(uploadError(file('portada.psd'))).toBe('Formato no compatible');
		expect(uploadError(file('a.wav', 0))).toBe('Archivo vacío');
		const big = { name: 'a.wav', size: UPLOAD_MAX_BYTES + 1 } as File;
		expect(uploadError(big)).toBe('Supera 500 MB');
	});
});

describe('summarizeQueue', () => {
	it('counts ready and failed items', () => {
		const items = [file('a.wav'), file('b.mp3'), file('c.psd')].map(toQueuedUpload);
		expect(summarizeQueue(items)).toEqual({
			ready: 2,
			failed: 1,
			uploading: 0,
			processing: 0,
			done: 0,
			errored: 0
		});
	});
});

describe('uploadLimitsLabel', () => {
	it('lists the formats and the size limit', () => {
		expect(uploadLimitsLabel()).toBe(
			'WAV, FLAC, AIFF o MP3 · hasta 500 MB por archivo · puedes soltar carpetas enteras'
		);
	});
});

describe('upload state machine', () => {
	const fresh = () => toQueuedUpload(file('a.wav'));

	it('walks queued to done', () => {
		let item = fresh();
		expect(item.phase).toBe('queued');
		item = beginUpload(item);
		expect(item.phase).toBe('uploading');
		item = reportProgress(item, 0.4);
		expect(item.progress).toBe(0.4);
		item = finishUpload(item);
		expect(item).toMatchObject({ phase: 'creating', progress: 1 });
		item = finishCreate(item, 't1');
		expect(item).toMatchObject({ phase: 'processing', trackId: 't1' });
		item = finishProcessing(item);
		expect(item.phase).toBe('done');
	});

	it('ignores transitions from the wrong phase', () => {
		const item = fresh();
		expect(finishUpload(item)).toBe(item);
		expect(finishCreate(item, 't1')).toBe(item);
		expect(finishProcessing(item)).toBe(item);
		expect(failUpload(item, 'x')).toBe(item);
		expect(retryUpload(item)).toBe(item);
	});

	it('never starts an invalid file', () => {
		const item = toQueuedUpload(file('a.psd'));
		expect(beginUpload(item)).toBe(item);
	});

	it('keeps progress monotonic and capped at 1', () => {
		let item = beginUpload(fresh());
		item = reportProgress(item, 0.6);
		expect(reportProgress(item, 0.3)).toBe(item);
		expect(reportProgress(item, 4).progress).toBe(1);
	});

	it('fails and retries', () => {
		let item = beginUpload(fresh());
		item = failUpload(item, 'Cancelada');
		expect(item).toMatchObject({ phase: 'error', failure: 'Cancelada' });
		item = retryUpload(item);
		expect(item).toMatchObject({ phase: 'queued', progress: 0, failure: null });
	});

	it('carries a note when the album step fails', () => {
		const item = finishCreate(finishUpload(beginUpload(fresh())), 't1', 'Sin álbum');
		expect(item.note).toBe('Sin álbum');
	});
});

describe('chunkStartable', () => {
	it('splits startable items into batches and skips the rest', () => {
		const items = ['a.wav', 'b.wav', 'c.wav', 'd.psd'].map((name) => toQueuedUpload(file(name)));
		items[1] = beginUpload(items[1]);
		const chunks = chunkStartable(items, 1);
		expect(chunks.map((chunk) => chunk.map((item) => item.file.name))).toEqual([
			['a.wav'],
			['c.wav']
		]);
	});
});

describe('hasActiveUploads', () => {
	it('is true while uploading, creating or processing', () => {
		const item = toQueuedUpload(file('a.wav'));
		expect(hasActiveUploads([item])).toBe(false);
		expect(hasActiveUploads([beginUpload(item)])).toBe(true);
	});
});

describe('genreConflictMessage', () => {
	const options = [
		{ genre: 'Rock', incompatibleWith: ['Classical'] },
		{ genre: 'Classical', incompatibleWith: ['Rock'] },
		{ genre: 'Jazz', incompatibleWith: [] }
	];

	it('names the conflicting genres', () => {
		expect(genreConflictMessage(['Rock', 'Classical'], options)).toBe(
			'Los géneros Rock y Clásica no se pueden combinar.'
		);
	});

	it('returns nothing when compatible', () => {
		expect(genreConflictMessage(['Rock', 'Jazz'], options)).toBeUndefined();
	});
});
