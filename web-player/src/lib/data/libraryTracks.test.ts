import { describe, expect, it } from 'vitest';
import type { LibraryTrack } from '$lib/types';
import { filterLibraryTracks } from './libraryTracks';

function track(id: string, title: string, status: LibraryTrack['status']): LibraryTrack {
	return {
		id,
		title,
		artist: 'Artista',
		ownerUserId: '1',
		explicit: false,
		duration: 60,
		listensCount: 0,
		date: '2026-01-01T00:00:00Z',
		status,
		genres: [],
		albumTitle: null
	};
}

const tracks = [
	track('1', 'Alba', 'Completed'),
	track('2', 'Noche', 'Processing'),
	track('3', 'Ruido', 'Pending'),
	track('4', 'Roto', 'Failed')
];

describe('filterLibraryTracks', () => {
	it('returns everything for the "all" filter without a query', () => {
		expect(filterLibraryTracks(tracks, '', 'all')).toHaveLength(4);
	});
	it('groups pending and processing tracks together', () => {
		expect(filterLibraryTracks(tracks, '', 'processing').map((t) => t.id)).toEqual(['2', '3']);
	});
	it('combines the status filter with the text query', () => {
		expect(filterLibraryTracks(tracks, ' alb ', 'published').map((t) => t.id)).toEqual(['1']);
		expect(filterLibraryTracks(tracks, 'alb', 'failed')).toEqual([]);
	});
	it('matches the artist too', () => {
		expect(filterLibraryTracks(tracks, 'artista', 'all')).toHaveLength(4);
	});
});
