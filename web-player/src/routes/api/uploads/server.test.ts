import { describe, expect, it, vi } from 'vitest';

vi.mock('$lib/server/config', () => ({ apiConfig: { baseUrl: 'http://api.test' } }));

import { POST } from './+server';
import { POST as COMPLETE } from './complete/+server';
import { GET as STATUS } from './status/+server';

type PostEvent = Parameters<typeof POST>[0];
type CompleteEvent = Parameters<typeof COMPLETE>[0];
type StatusEvent = Parameters<typeof STATUS>[0];

function event(body: unknown, token: string | null = 'token') {
	return {
		request: new Request('http://x/api/uploads', { method: 'POST', body: JSON.stringify(body) }),
		locals: { accessToken: token, user: null },
		fetch: vi.fn()
	};
}

describe('POST /api/uploads', () => {
	it('requires a session', async () => {
		const res = await POST(event({}, null) as unknown as PostEvent);
		expect(res.status).toBe(401);
	});

	it('rejects an invalid batch', async () => {
		const res = await POST(event({ files: [] }) as unknown as PostEvent);
		expect(res.status).toBe(400);
	});

	it('proxies the presign request to the API', async () => {
		const ev = event({ cover: null, files: [{ fileName: 'a.wav', contentType: '', size: 4 }] });
		ev.fetch.mockResolvedValue(
			Response.json({
				pictureUploadUrl: null,
				pictureContentType: null,
				audios: [{ intentId: 'i', pictureIntentId: null, uploadUrl: 'u', contentType: 'audio/wav' }]
			})
		);
		const res = await POST(ev as unknown as PostEvent);
		expect(res.status).toBe(200);
		expect(await res.json()).toMatchObject({ picture: null, audios: [{ intentId: 'i' }] });
	});
});

describe('POST /api/uploads/complete', () => {
	it('requires a session', async () => {
		const res = await COMPLETE(event({}, null) as unknown as CompleteEvent);
		expect(res.status).toBe(401);
	});

	it('rejects a missing title', async () => {
		const res = await COMPLETE(
			event({ title: '', audioIntentId: 'a', tags: ['Rock'] }) as unknown as CompleteEvent
		);
		expect(res.status).toBe(400);
	});
});

describe('GET /api/uploads/status', () => {
	it('requires a session', async () => {
		const res = await STATUS({
			url: new URL('http://x/api/uploads/status?ids=a'),
			locals: { accessToken: null, user: null },
			fetch: vi.fn()
		} as unknown as StatusEvent);
		expect(res.status).toBe(401);
	});

	it('rejects missing ids', async () => {
		const res = await STATUS({
			url: new URL('http://x/api/uploads/status'),
			locals: { accessToken: 't', user: null },
			fetch: vi.fn()
		} as unknown as StatusEvent);
		expect(res.status).toBe(400);
	});
});
