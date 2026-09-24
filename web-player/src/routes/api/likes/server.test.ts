import { describe, expect, it, vi } from 'vitest';

vi.mock('$lib/server/config', () => ({ apiConfig: { baseUrl: 'http://api.test' } }));

import { POST } from './+server';

type PostEvent = Parameters<typeof POST>[0];

function event(body: unknown, options: { token?: string | null; api?: Response } = {}) {
	const { token = 'token', api = Response.json(true) } = options;
	const fetchFn = vi.fn().mockResolvedValue(api);
	const request = new Request('http://x/api/likes', {
		method: 'POST',
		headers: { 'content-type': 'application/json' },
		body: typeof body === 'string' ? body : JSON.stringify(body)
	});
	return {
		event: {
			request,
			locals: { accessToken: token, user: null },
			fetch: fetchFn
		} as unknown as PostEvent,
		fetchFn
	};
}

describe('POST /api/likes', () => {
	it('returns 401 without a token', async () => {
		const { event: e, fetchFn } = event({ trackId: 't1' }, { token: null });
		expect((await POST(e)).status).toBe(401);
		expect(fetchFn).not.toHaveBeenCalled();
	});

	it.each([[{}], [{ trackId: '' }], [{ trackId: 5 }], ['not json']])(
		'returns 400 for an invalid body %j',
		async (body) => {
			const { event: e, fetchFn } = event(body);
			expect((await POST(e)).status).toBe(400);
			expect(fetchFn).not.toHaveBeenCalled();
		}
	);

	it('returns the liked state from the API', async () => {
		const { event: e } = event({ trackId: 't1' });
		const res = await POST(e);
		expect(res.status).toBe(200);
		expect(await res.json()).toEqual({ liked: true });
	});

	it('returns 502 when the API fails', async () => {
		const { event: e } = event(
			{ trackId: 't1' },
			{ api: Response.json({ title: 'boom' }, { status: 500 }) }
		);
		expect((await POST(e)).status).toBe(502);
	});
});
