import { describe, expect, it } from 'vitest';
import { albumMeta, fmtDurationLong, fmtTime, plural } from './format';

describe('fmtTime', () => {
	it('formats minutes and padded seconds', () => {
		expect(fmtTime(0)).toBe('0:00');
		expect(fmtTime(65)).toBe('1:05');
		expect(fmtTime(600)).toBe('10:00');
	});
	it('floors fractions and clamps invalid input', () => {
		expect(fmtTime(59.9)).toBe('0:59');
		expect(fmtTime(-5)).toBe('0:00');
		expect(fmtTime(NaN)).toBe('0:00');
	});
});

describe('fmtDurationLong', () => {
	it('shows minutes only under an hour', () => {
		expect(fmtDurationLong(59)).toBe('0 Min');
		expect(fmtDurationLong(125)).toBe('2 Min');
	});
	it('shows hours and minutes', () => {
		expect(fmtDurationLong(3600)).toBe('1 H 0 Min');
		expect(fmtDurationLong(3600 * 2 + 60 * 5)).toBe('2 H 5 Min');
	});
});

describe('plural', () => {
	it('uses the singular only for 1', () => {
		expect(plural(1, 'canción', 'canciones')).toBe('1 canción');
		expect(plural(0, 'canción', 'canciones')).toBe('0 canciones');
		expect(plural(2, 'canción', 'canciones')).toBe('2 canciones');
	});
});

describe('albumMeta', () => {
	it('joins year and track count', () => {
		expect(albumMeta(2020, 1)).toBe('2020 · 1 canción');
		expect(albumMeta(2020, 12)).toBe('2020 · 12 canciones');
	});
	it('omits missing parts', () => {
		expect(albumMeta(undefined, 3)).toBe('3 canciones');
		expect(albumMeta(2020)).toBe('2020');
		expect(albumMeta()).toBe('');
	});
});
