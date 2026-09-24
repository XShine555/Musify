import { describe, expect, it, vi } from 'vitest';
import { UploadQueue } from '$lib/state/uploadQueue.svelte';
import type { UploadTransport } from '$lib/utils/uploadTransport';
import { putHeaders } from '$lib/utils/uploadTransport';
import type { GenreOption } from '$lib/types';

const GENRES: GenreOption[] = [
	{ genre: 'Rock', incompatibleWith: ['Classical'] },
	{ genre: 'Classical', incompatibleWith: ['Rock'] },
	{ genre: 'Jazz', incompatibleWith: [] }
];

function file(name: string) {
	return new File([new Uint8Array(8)], name);
}

function fakeTransport(overrides: Partial<UploadTransport> = {}) {
	let counter = 0;
	const transport: UploadTransport = {
		presign: vi.fn(async (request) => ({
			picture: request.cover ? { uploadUrl: 'https://s3/pic', contentType: 'image/png' } : null,
			audios: request.files.map(() => ({
				intentId: `intent-${counter++}`,
				pictureIntentId: request.cover ? 'pic' : null,
				uploadUrl: 'https://s3/audio',
				contentType: 'audio/wav'
			}))
		})),
		put: vi.fn(async (_target, _file, onProgress) => onProgress(0.5)),
		complete: vi.fn(async (request) => ({ trackId: `track-${request.audioIntentId}`, note: null })),
		status: vi.fn(async (ids) =>
			Object.fromEntries(ids.map((id: string) => [id, 'Completed' as const]))
		),
		...overrides
	};
	return transport;
}

function queue(transport: UploadTransport) {
	const q = new UploadQueue({
		transport,
		pollDelay: async () => {},
		readDuration: async () => 60
	});
	q.selectedGenres = ['Jazz'];
	return q;
}

describe('UploadQueue', () => {
	it('uploads every valid file, creates the tracks and finishes them', async () => {
		const transport = fakeTransport();
		const q = queue(transport);
		q.enqueue([file('a.wav'), file('b.mp3'), file('c.psd')]);
		await q.start(GENRES);
		await vi.waitFor(() => expect(q.summary.done).toBe(2));
		expect(q.summary.failed).toBe(1);
		expect(transport.put).toHaveBeenCalledTimes(2);
		expect(transport.complete).toHaveBeenCalledTimes(2);
		expect(transport.presign).toHaveBeenCalledTimes(1);
	});

	it('uploads the shared cover once and passes the album and genres', async () => {
		const transport = fakeTransport();
		const q = queue(transport);
		q.cover = new File([new Uint8Array(4)], 'c.png', { type: 'image/png' });
		q.albumId = 'album-1';
		q.enqueue([file('a.wav'), file('b.wav')]);
		await q.start(GENRES);
		const puts = vi.mocked(transport.put).mock.calls.map((call) => call[0].uploadUrl);
		expect(puts.filter((url) => url === 'https://s3/pic')).toHaveLength(1);
		expect(transport.complete).toHaveBeenCalledWith(
			expect.objectContaining({ albumId: 'album-1', tags: ['Jazz'], pictureIntentId: 'pic' }),
			expect.anything()
		);
	});

	it('requires genres and rejects conflicting ones before sending anything', async () => {
		const transport = fakeTransport();
		const q = queue(transport);
		q.enqueue([file('a.wav')]);
		q.selectedGenres = [];
		await q.start(GENRES);
		expect(q.notice).toContain('género');
		q.selectedGenres = ['Rock', 'Classical'];
		await q.start(GENRES);
		expect(q.notice).toContain('no se pueden combinar');
		expect(transport.presign).not.toHaveBeenCalled();
	});

	it('never runs more than the concurrency limit at once', async () => {
		let running = 0;
		let peak = 0;
		const transport = fakeTransport({
			put: async () => {
				running++;
				peak = Math.max(peak, running);
				await new Promise((resolve) => setTimeout(resolve, 5));
				running--;
			}
		});
		const q = queue(transport);
		q.enqueue(Array.from({ length: 8 }, (_, i) => file(`${i}.wav`)));
		await q.start(GENRES);
		expect(peak).toBe(3);
	});

	it('marks a file as failed and lets it retry', async () => {
		let fail = true;
		const transport = fakeTransport({
			put: async () => {
				if (fail) throw new Error('boom');
			}
		});
		const q = queue(transport);
		q.enqueue([file('a.wav')]);
		await q.start(GENRES);
		expect(q.items[0]).toMatchObject({ phase: 'error', failure: 'boom' });
		fail = false;
		q.retry(q.items[0].id);
		await q.start(GENRES);
		await vi.waitFor(() => expect(q.items[0].phase).toBe('done'));
	});

	it('fails the whole batch when the presign request fails', async () => {
		const transport = fakeTransport({
			presign: async () => {
				throw new Error('sin URLs');
			}
		});
		const q = queue(transport);
		q.enqueue([file('a.wav'), file('b.wav')]);
		await q.start(GENRES);
		expect(q.items.map((item) => item.failure)).toEqual(['sin URLs', 'sin URLs']);
	});

	it('cancels an upload in flight', async () => {
		const transport = fakeTransport({
			put: (_target, _file, _progress, signal) =>
				new Promise((_resolve, reject) =>
					signal.addEventListener('abort', () => reject(new DOMException('x', 'AbortError')))
				)
		});
		const q = queue(transport);
		q.enqueue([file('a.wav')]);
		const started = q.start(GENRES);
		await vi.waitFor(() => expect(q.items[0].phase).toBe('uploading'));
		q.cancel(q.items[0].id);
		await started;
		expect(q.items[0]).toMatchObject({ phase: 'error', failure: 'Cancelada' });
		expect(transport.complete).not.toHaveBeenCalled();
	});

	it('keeps the album note and turns a failed processing into an error', async () => {
		const transport = fakeTransport({
			complete: async () => ({ trackId: 't1', note: 'Sin álbum' }),
			status: async () => ({ t1: 'Failed' })
		});
		const q = queue(transport);
		q.enqueue([file('a.wav')]);
		await q.start(GENRES);
		await vi.waitFor(() => expect(q.items[0].phase).toBe('error'));
		expect(q.items[0].note).toBe('Sin álbum');
	});

	it('reports active uploads and clears the queue', async () => {
		const transport = fakeTransport({
			put: (_target, _file, _progress, signal) =>
				new Promise((_resolve, reject) =>
					signal.addEventListener('abort', () => reject(new DOMException('x', 'AbortError')))
				)
		});
		const q = queue(transport);
		q.enqueue([file('a.wav')]);
		const started = q.start(GENRES);
		await vi.waitFor(() => expect(q.active).toBe(true));
		q.clear();
		await started;
		expect(q.items).toEqual([]);
		expect(q.active).toBe(false);
	});
});

describe('putHeaders', () => {
	it('sends only the signed headers', () => {
		const target = { uploadUrl: 'https://s3/a?X-Amz-SignedHeaders=host', contentType: 'audio/wav' };
		expect(putHeaders(target)).toEqual({});
		expect(
			putHeaders({
				...target,
				uploadUrl: 'https://s3/a?X-Amz-SignedHeaders=content-type%3Bhost%3Bif-none-match'
			})
		).toEqual({ 'Content-Type': 'audio/wav', 'If-None-Match': '*' });
	});
});
