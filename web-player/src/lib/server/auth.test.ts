import { describe, expect, it } from 'vitest';
import { safeReturnTo } from './auth';

describe('safeReturnTo', () => {
	it('keeps local paths', () => {
		expect(safeReturnTo('/')).toBe('/');
		expect(safeReturnTo('/playlists/1?tab=x#top')).toBe('/playlists/1?tab=x#top');
	});
	it('falls back to / for missing values', () => {
		expect(safeReturnTo(null)).toBe('/');
		expect(safeReturnTo(undefined)).toBe('/');
		expect(safeReturnTo('')).toBe('/');
	});
	it('rejects absolute and protocol-relative URLs', () => {
		expect(safeReturnTo('https://example.com')).toBe('/');
		expect(safeReturnTo('//example.com')).toBe('/');
		expect(safeReturnTo('/\\example.com')).toBe('/');
		expect(safeReturnTo('javascript:alert(1)')).toBe('/');
		expect(safeReturnTo('relative/path')).toBe('/');
	});
});
