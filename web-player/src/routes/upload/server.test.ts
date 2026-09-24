import { describe, expect, it } from 'vitest';
import { GET } from './+server';

describe('GET /upload', () => {
	it('redirects permanently to the uploads section', () => {
		try {
			GET({} as Parameters<typeof GET>[0]);
			expect.unreachable();
		} catch (err) {
			expect(err).toMatchObject({ status: 308, location: '/library/uploads' });
		}
	});
});
