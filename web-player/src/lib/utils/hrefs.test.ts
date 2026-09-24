import { describe, expect, it } from 'vitest';
import { albumCover, genreHref, playlistCover, searchHref, trackCover } from './hrefs';

describe('hrefs', () => {
	it('builds search and genre links', () => {
		expect(searchHref('')).toBe('/explore');
		expect(searchHref('a b')).toBe('/explore?q=a%20b');
		expect(genreHref('hip hop')).toBe('/explore?genre=hip%20hop');
	});
	it('builds cover urls', () => {
		expect(trackCover('t', 'small')).toBe('/api/tracks/t/cover?size=small');
		expect(albumCover('a', 'large')).toBe('/api/albums/a/cover?size=large');
		expect(playlistCover({ id: 'p', updatedAt: '2026-01-01T00:00:00Z' }, 'medium')).toBe(
			'/api/playlists/p/cover?size=medium&v=2026-01-01T00%3A00%3A00Z'
		);
	});
});
