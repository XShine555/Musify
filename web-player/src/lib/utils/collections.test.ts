import { describe, expect, it } from 'vitest';
import { appendUnique, shuffle } from './collections';

describe('appendUnique', () => {
	const keyOf = (item: { id: number }) => item.id;
	it('appends only unseen keys', () => {
		const result = appendUnique([{ id: 1 }], [{ id: 1 }, { id: 2 }, { id: 2 }, { id: 3 }], keyOf);
		expect(result).toEqual([{ id: 1 }, { id: 2 }, { id: 3 }]);
	});
	it('returns the same array when nothing is new', () => {
		const current = [{ id: 1 }];
		expect(appendUnique(current, [{ id: 1 }], keyOf)).toBe(current);
	});
});

describe('shuffle', () => {
	it('keeps the same elements and does not mutate the input', () => {
		const input = [1, 2, 3, 4, 5, 6];
		const result = shuffle(input);
		expect([...result].sort()).toEqual(input);
		expect(input).toEqual([1, 2, 3, 4, 5, 6]);
	});
});
