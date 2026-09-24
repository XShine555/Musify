import { isActionFailure } from '@sveltejs/kit';
import { describe, expect, it, vi } from 'vitest';
import type { ApiClient } from '$lib/server/api';
import { deleteTracks, setTracksPublished } from '$lib/server/tracks';

describe('deleteTracks', () => {
	it('counts the tracks the API could not delete', async () => {
		const DELETE = vi.fn(async () => ({
			data: { deletedTrackIds: ['a', 'c'], failedTrackIds: ['b'] }
		}));
		const result = await deleteTracks({ DELETE } as unknown as ApiClient, ['a', 'b', 'c']);
		expect(result).toEqual({ failed: 1 });
	});

	it('counts a whole batch as failed when the request errors', async () => {
		const DELETE = vi.fn(async () => ({ error: { status: 500 } }));
		expect(await deleteTracks({ DELETE } as unknown as ApiClient, ['a', 'b'])).toEqual({
			failed: 2
		});
	});

	it('splits large deletions into batches of 100', async () => {
		const DELETE = vi.fn(async () => ({ data: { deletedTrackIds: [], failedTrackIds: [] } }));
		const ids = Array.from({ length: 250 }, (_, i) => String(i));
		await deleteTracks({ DELETE } as unknown as ApiClient, ids);
		expect(DELETE).toHaveBeenCalledTimes(3);
	});
});

describe('setTracksPublished', () => {
	it('reports updated and skipped tracks', async () => {
		const PUT = vi.fn(async () => ({
			data: { updatedTrackIds: ['a'], skippedTrackIds: ['b'] }
		}));
		const result = await setTracksPublished({ PUT } as unknown as ApiClient, ['a', 'b'], true);
		expect(result).toEqual({ updated: 1, skipped: 1 });
	});

	it('returns a failure when the request errors', async () => {
		const PUT = vi.fn(async () => ({ error: { status: 400 } }));
		const result = await setTracksPublished({ PUT } as unknown as ApiClient, ['a'], true);
		expect(isActionFailure(result)).toBe(true);
	});
});
