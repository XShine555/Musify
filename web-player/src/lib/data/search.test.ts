import { describe, expect, it } from 'vitest';
import { findTopResult } from './search';

const empty = { tracks: [], albums: [], playlists: [], users: [] };

describe('findTopResult', () => {
	it('prefers prefix matches by kind: user, track, album, playlist', () => {
		const result = findTopResult({
			query: 'Ab',
			users: [{ name: 'Zed' }, { name: 'abel' }],
			tracks: [{ title: 'Abc' }],
			albums: [{ title: 'Abd' }],
			playlists: [{ name: 'Abe' }]
		});
		expect(result).toEqual({ kind: 'user', user: { name: 'abel' } });
	});
	it('matches case-insensitively and ignores surrounding spaces', () => {
		const result = findTopResult({ ...empty, query: '  SO ', tracks: [{ title: 'song' }] });
		expect(result).toEqual({ kind: 'track', track: { title: 'song' } });
	});
	it('falls back to the first available result', () => {
		const result = findTopResult({ ...empty, query: 'zzz', albums: [{ title: 'Other' }] });
		expect(result).toEqual({ kind: 'album', album: { title: 'Other' } });
	});
	it('returns null without results', () => {
		expect(findTopResult({ ...empty, query: 'x' })).toBeNull();
	});
});
