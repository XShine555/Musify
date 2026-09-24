import { describe, expect, it, vi } from 'vitest';

vi.mock('$lib/server/config', () => ({ apiConfig: { baseUrl: 'http://api.test' } }));

import { PUT } from './+server';

type PutEvent = Parameters<typeof PUT>[0];

function event(body: string, api: Response = new Response(null, { status: 204 })) {
	const fetchFn = vi.fn().mockResolvedValue(api);
	const request = new Request('http://x/api/listens/l1/progress', {
		method: 'PUT',
		headers: { 'content-type': 'application/json' },
		body
	});
	return {
		event: {
			request,
			params: { id: 'l1' },
			locals: { accessToken: 'token', user: null },
			fetch: fetchFn
		} as unknown as PutEvent,
		fetchFn
	};
}

describe('PUT /api/listens/[id]/progress', () => {
	it('returns 401 without a token', async () => {
		const { event: e } = event('{"playedSeconds":5}');
		(e.locals as { accessToken: string | null }).accessToken = null;
		expect((await PUT(e)).status).toBe(401);
	});

	it.each([
		['invalid JSON', 'nope'],
		['negative seconds', '{"playedSeconds":-1}'],
		['string seconds', '{"playedSeconds":"5"}'],
		['missing seconds', '{}'],
		['null seconds', '{"playedSeconds":null}']
	])('returns 400 for %s', async (_name, body) => {
		const { event: e, fetchFn } = event(body);
		expect((await PUT(e)).status).toBe(400);
		expect(fetchFn).not.toHaveBeenCalled();
	});

	it('returns 204 when the API accepts the progress', async () => {
		const { event: e, fetchFn } = event('{"playedSeconds":12.5}');
		expect((await PUT(e)).status).toBe(204);
		expect(fetchFn).toHaveBeenCalledTimes(1);
	});

	it('returns 404 when the API returns 404', async () => {
		const { event: e } = event('{"playedSeconds":5}', Response.json({}, { status: 404 }));
		expect((await PUT(e)).status).toBe(404);
	});

	it('returns 502 for other API failures', async () => {
		const { event: e } = event('{"playedSeconds":5}', Response.json({}, { status: 500 }));
		expect((await PUT(e)).status).toBe(502);
	});
});
