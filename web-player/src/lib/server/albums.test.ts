import { describe, expect, it, vi } from 'vitest';
import type { ApiClient } from '$lib/server/api';
import { syncAlbumTracks } from '$lib/server/albums';

function fakeApi(overrides: { post?: unknown; remove?: (trackId: string) => unknown } = {}) {
	return {
		POST: vi.fn(async () => overrides.post ?? {}),
		DELETE: vi.fn(async (_path: string, init: { params: { path: { trackId: string } } }) =>
			overrides.remove ? overrides.remove(init.params.path.trackId) : {}
		)
	};
}

describe('syncAlbumTracks', () => {
	it('adds and removes tracks', async () => {
		const api = fakeApi();
		const failure = await syncAlbumTracks(api as unknown as ApiClient, 'album', ['a'], ['b', 'c']);
		expect(failure).toBeUndefined();
		expect(api.POST).toHaveBeenCalledTimes(1);
		expect(api.DELETE).toHaveBeenCalledTimes(2);
	});

	it('skips the add request when there is nothing to add', async () => {
		const api = fakeApi();
		await syncAlbumTracks(api as unknown as ApiClient, 'album', [], ['b']);
		expect(api.POST).not.toHaveBeenCalled();
	});

	it('fails without removing when adding fails', async () => {
		const api = fakeApi({ post: { error: { status: 500 } } });
		const failure = await syncAlbumTracks(api as unknown as ApiClient, 'album', ['a'], ['b']);
		expect(failure?.data?.message).toBe('No se pudieron añadir las canciones.');
		expect(api.DELETE).not.toHaveBeenCalled();
	});

	it('fails when a removal fails', async () => {
		const api = fakeApi({ remove: (id) => (id === 'c' ? { error: { status: 500 } } : {}) });
		const failure = await syncAlbumTracks(api as unknown as ApiClient, 'album', [], ['b', 'c']);
		expect(failure?.data?.message).toBe('No se pudieron quitar las canciones.');
	});
});
