import { beforeEach, describe, expect, it, vi } from 'vitest';

const allowAnonymous = vi.hoisted(() => vi.fn());

vi.mock('$lib/server/playbackConfig', () => ({ getAllowAnonymousListening: allowAnonymous }));
vi.mock('$lib/server/config', () => ({
	authConfig: { issuer: 'https://auth.example.com/' },
	apiConfig: { baseUrl: 'http://api.test' }
}));

import { load } from './+layout.server';

type LoadEvent = Parameters<typeof load>[0];

function event(user: { sub: string; name: string } | null, fetchFn = vi.fn()) {
	return {
		locals: { user, accessToken: user ? 'token' : null },
		url: new URL('http://x/library?q=1'),
		fetch: fetchFn
	} as unknown as LoadEvent;
}

beforeEach(() => {
	allowAnonymous.mockReset();
});

describe('(app) layout load', () => {
	it('redirects to login without a session or anonymous listening', async () => {
		allowAnonymous.mockResolvedValue(false);
		await expect(load(event(null))).rejects.toMatchObject({
			status: 302,
			location: `/auth?returnTo=${encodeURIComponent('/library?q=1')}`
		});
	});

	it('returns empty data in anonymous mode without calling the API', async () => {
		allowAnonymous.mockResolvedValue(true);
		const fetchFn = vi.fn();
		const data = await load(event(null, fetchFn));
		expect(data).toMatchObject({
			user: null,
			allowAnonymousListening: true,
			userPlaylists: [],
			userPlaylistsTotal: 0,
			likedTracks: [],
			lastPlayedTrack: null,
			accountUrl: 'https://auth.example.com/ui/console'
		});
		expect(fetchFn).not.toHaveBeenCalled();
	});

	it('falls back to empty data when the API fails for a signed in user', async () => {
		allowAnonymous.mockResolvedValue(false);
		const fetchFn = vi.fn().mockRejectedValue(new Error('down'));
		const data = await load(event({ sub: 'u1', name: 'Ana' }, fetchFn));
		expect(data).toMatchObject({
			user: { sub: 'u1' },
			userPlaylists: [],
			userPlaylistsTotal: 0,
			likedTracks: [],
			lastPlayedTrack: null
		});
	});
});
