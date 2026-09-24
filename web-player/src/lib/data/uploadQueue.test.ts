import { describe, expect, it } from 'vitest';
import {
	summarizeQueue,
	titleFromFileName,
	toQueuedUpload,
	uploadError,
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
		expect(summarizeQueue(items)).toEqual({ ready: 2, failed: 1 });
	});
});
