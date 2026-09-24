import { afterEach, describe, expect, it, vi } from 'vitest';
import type { LikedTrack, Track } from '$lib/types';

function track(id: string): Track {
	return {
		id,
		title: id,
		artist: 'Artist',
		duration: 100,
		explicit: false,
		listensCount: 0
	} as unknown as Track;
}

function likedTrack(id: string): LikedTrack {
	return { ...track(id), likedAt: 1 } as LikedTrack;
}

async function freshStore() {
	vi.resetModules();
	return (await import('./liked.svelte')).liked;
}

afterEach(() => {
	vi.unstubAllGlobals();
});

describe('liked store', () => {
	it('hydrates only once', async () => {
		const liked = await freshStore();
		liked.hydrate([likedTrack('a')]);
		liked.hydrate([likedTrack('b')]);
		expect(liked.isLiked('a')).toBe(true);
		expect(liked.isLiked('b')).toBe(false);
		expect(liked.count).toBe(1);
	});

	it('likes and unlikes a track', async () => {
		vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true }));
		const liked = await freshStore();
		await liked.toggle(track('a'));
		expect(liked.isLiked('a')).toBe(true);
		await liked.toggle(track('a'));
		expect(liked.isLiked('a')).toBe(false);
		expect(fetch).toHaveBeenCalledTimes(2);
	});

	it('reverts a failed like', async () => {
		vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false }));
		const liked = await freshStore();
		await liked.toggle(track('a'));
		expect(liked.isLiked('a')).toBe(false);
	});

	it('reverts a failed unlike', async () => {
		const liked = await freshStore();
		liked.hydrate([likedTrack('a')]);
		vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline')));
		await liked.toggle(track('a'));
		expect(liked.isLiked('a')).toBe(true);
	});

	it('keeps changes made to other tracks while a request fails', async () => {
		const liked = await freshStore();
		let rejectFirst: (reason: Error) => void = () => {};
		const fetchMock = vi
			.fn()
			.mockImplementationOnce(() => new Promise((_, reject) => (rejectFirst = reject)))
			.mockResolvedValueOnce({ ok: true });
		vi.stubGlobal('fetch', fetchMock);

		const first = liked.toggle(track('a'));
		await liked.toggle(track('b'));
		rejectFirst(new Error('offline'));
		await first;

		expect(liked.isLiked('a')).toBe(false);
		expect(liked.isLiked('b')).toBe(true);
	});
});
