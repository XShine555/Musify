import { describe, expect, it } from 'vitest';
import { findConflict, genreHue } from './genres';

describe('genreHue', () => {
	it('is stable and within 0-359', () => {
		for (const genre of ['rock', 'something-unknown', 'ñandú']) {
			const hue = genreHue(genre);
			expect(hue).toBe(genreHue(genre));
			expect(hue).toBeGreaterThanOrEqual(0);
			expect(hue).toBeLessThan(360);
		}
	});
});

describe('findConflict', () => {
	const options = [
		{ genre: 'a', incompatibleWith: ['b'] },
		{ genre: 'b', incompatibleWith: ['a'] },
		{ genre: 'c', incompatibleWith: [] }
	];
	it('returns the first incompatible pair', () => {
		expect(findConflict(['c', 'a', 'b'], options)).toEqual(['a', 'b']);
	});
	it('returns undefined when compatible or unknown', () => {
		expect(findConflict(['a', 'c'], options)).toBeUndefined();
		expect(findConflict(['x', 'y'], options)).toBeUndefined();
	});
});
