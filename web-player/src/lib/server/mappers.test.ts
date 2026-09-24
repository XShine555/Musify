import { describe, expect, it } from 'vitest';
import type { components } from '$lib/api/schema';
import {
	toAlbum,
	toDatedTrack,
	toLikedTrack,
	toMix,
	toPage,
	toPlaylist,
	toPlaylistSummary,
	toTrack
} from './mappers';

const trackDto: components['schemas']['TrackApplicationResponse'] = {
	id: 't1',
	title: 'Song',
	artist: null,
	audioStatus: 'Completed',
	duration: '183.5',
	listensCount: '12',
	createdAt: '2026-01-02T00:00:00Z',
	updatedAt: '2026-01-03T00:00:00Z',
	ownerUserId: '42',
	tags: [],
	isExplicit: true
};

describe('toTrack', () => {
	it('normalizes numeric strings and renames isExplicit', () => {
		expect(toTrack(trackDto)).toEqual({
			id: 't1',
			title: 'Song',
			artist: null,
			ownerUserId: '42',
			explicit: true,
			duration: 183.5,
			listensCount: 12
		});
	});
	it('falls back to 0 for invalid numbers', () => {
		const track = toTrack({ ...trackDto, duration: 'x', listensCount: 'y' });
		expect(track.duration).toBe(0);
		expect(track.listensCount).toBe(0);
	});
});

describe('toDatedTrack and toLikedTrack', () => {
	it('keep the creation date', () => {
		expect(toDatedTrack(trackDto).date).toBe('2026-01-02T00:00:00Z');
		expect(toLikedTrack(trackDto).likedAt).toBe(Date.parse('2026-01-02T00:00:00Z'));
	});
});

describe('toMix', () => {
	it('maps items into tracks', () => {
		const mix = toMix({
			id: 'm1',
			title: 'Mix',
			subtitle: null,
			itemCount: 1,
			items: [{ trackId: 't1', title: 'Song', artist: 'A', durationSeconds: '60', listensCount: 3 }]
		});
		expect(mix.tracks).toEqual([
			{
				id: 't1',
				title: 'Song',
				artist: 'A',
				ownerUserId: null,
				explicit: false,
				duration: 60,
				listensCount: 3
			}
		]);
	});
});

describe('toAlbum', () => {
	const dto: components['schemas']['AlbumApplicationResponse'] = {
		id: 'a1',
		title: 'Album',
		description: null,
		releaseYear: '2020',
		ownerUserId: '42',
		trackCount: '5',
		smallImageKeyName: null,
		mediumImageKeyName: null,
		largeImageKeyName: null,
		createdAt: '',
		updatedAt: 'u',
		coverTrackIds: ['t1']
	};
	it('normalizes year and count', () => {
		expect(toAlbum(dto)).toMatchObject({ releaseYear: 2020, trackCount: 5, updatedAt: 'u' });
	});
	it('keeps a missing year as null', () => {
		expect(toAlbum({ ...dto, releaseYear: null }).releaseYear).toBeNull();
	});
});

describe('toPlaylist', () => {
	const dto: components['schemas']['PlayListApplicationResponse'] = {
		id: 'p1',
		name: 'List',
		description: 'd',
		smallImageKeyName: null,
		mediumImageKeyName: null,
		largeImageKeyName: null,
		ownerUserId: '42',
		visibility: 'Public',
		createdAt: '',
		updatedAt: 'u',
		coverTrackIds: []
	};
	it('drops storage keys', () => {
		expect(toPlaylist(dto)).toEqual({
			id: 'p1',
			name: 'List',
			description: 'd',
			visibility: 'Public',
			ownerUserId: '42',
			coverTrackIds: [],
			updatedAt: 'u'
		});
	});
	it('adds the track count in summaries', () => {
		expect(toPlaylistSummary(dto, 7).trackCount).toBe(7);
	});
});

describe('toPage', () => {
	it('maps items and normalizes numbers', () => {
		expect(
			toPage(
				{ items: [1, 2], pageNumber: '2', hasNextPage: true, totalItemCount: '10' },
				(n) => n * 2
			)
		).toEqual({ items: [2, 4], pageNumber: 2, hasNextPage: true, totalItemCount: 10 });
	});
});
