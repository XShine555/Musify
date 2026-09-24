import { describe, expect, it, vi } from 'vitest';
import type { ApiClient } from '$lib/server/api';
import { deleteTracks } from '$lib/server/tracks';

describe('deleteTracks', () => {
	it('counts the deletions that failed', async () => {
		const DELETE = vi.fn(async (_path: string, init: { params: { path: { id: string } } }) =>
			init.params.path.id === 'b' ? { error: { status: 500 } } : {}
		);
		const result = await deleteTracks({ DELETE } as unknown as ApiClient, ['a', 'b', 'c']);
		expect(result).toEqual({ failed: 1 });
		expect(DELETE).toHaveBeenCalledTimes(3);
	});

	it('reports no failures when every deletion succeeds', async () => {
		const DELETE = vi.fn(async () => ({}));
		expect(await deleteTracks({ DELETE } as unknown as ApiClient, ['a'])).toEqual({ failed: 0 });
	});
});
