import { describe, expect, it, vi } from 'vitest';
import { EXPLORE_PAGE_SIZE, MAX_PAGE_SIZE } from '$lib/config';

vi.mock('$lib/server/config', () => ({ apiConfig: { baseUrl: 'http://api.test' } }));

import { GET } from './+server';

type GetEvent = Parameters<typeof GET>[0];

async function requestedQuery(search: string) {
	const fetchFn = vi
		.fn()
		.mockResolvedValue(
			Response.json({ items: [], pageNumber: 1, hasNextPage: false, totalItemCount: 0 })
		);
	const res = await GET({
		url: new URL(`http://x/api/tracks${search}`),
		locals: { accessToken: null, user: null },
		fetch: fetchFn
	} as unknown as GetEvent);
	expect(res.status).toBe(200);
	const request = fetchFn.mock.calls[0][0] as Request;
	return new URL(request.url).searchParams;
}

describe('GET /api/tracks', () => {
	it('caps pageSize at MAX_PAGE_SIZE', async () => {
		const query = await requestedQuery('?pageSize=100000');
		expect(query.get('pageSize')).toBe(String(MAX_PAGE_SIZE));
	});

	it('uses the default page size for invalid values', async () => {
		expect((await requestedQuery('?pageSize=abc')).get('pageSize')).toBe(String(EXPLORE_PAGE_SIZE));
		expect((await requestedQuery('')).get('pageSize')).toBe(String(EXPLORE_PAGE_SIZE));
	});

	it('resets an invalid pageNumber to 1', async () => {
		expect((await requestedQuery('?pageNumber=-3')).get('pageNumber')).toBe('1');
		expect((await requestedQuery('?pageNumber=abc')).get('pageNumber')).toBe('1');
		expect((await requestedQuery('?pageNumber=4')).get('pageNumber')).toBe('4');
	});
});
