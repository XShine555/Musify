import { describe, expect, it } from 'vitest';
import { append, insertNext, move, nextIndex, previousIndex } from './queue';

describe('insertNext', () => {
	it('inserts right after the given index without mutating', () => {
		const queue = ['a', 'b', 'c'];
		expect(insertNext(queue, 0, ['x', 'y'])).toEqual(['a', 'x', 'y', 'b', 'c']);
		expect(queue).toEqual(['a', 'b', 'c']);
	});
	it('inserts at the start when index is -1', () => {
		expect(insertNext(['a'], -1, ['x'])).toEqual(['x', 'a']);
	});
});

describe('append', () => {
	it('adds items at the end', () => {
		expect(append(['a'], ['b', 'c'])).toEqual(['a', 'b', 'c']);
	});
});

describe('move', () => {
	const queue = ['a', 'b', 'c', 'd'];
	it('moves an item forward using insert position semantics', () => {
		expect(move(queue, 0, 3)).toEqual(['b', 'c', 'a', 'd']);
	});
	it('moves an item backward', () => {
		expect(move(queue, 3, 1)).toEqual(['a', 'd', 'b', 'c']);
	});
	it('is a no-op when the target equals the origin', () => {
		expect(move(queue, 1, 1)).toBe(queue);
		expect(move(queue, 1, 2)).toBe(queue);
	});
	it('ignores out-of-range origins', () => {
		expect(move(queue, -1, 0)).toBe(queue);
		expect(move(queue, 4, 0)).toBe(queue);
	});
});

describe('nextIndex', () => {
	it('advances and wraps around', () => {
		expect(nextIndex(3, 0, false)).toBe(1);
		expect(nextIndex(3, 2, false)).toBe(0);
	});
	it('never repeats the current index when shuffling', () => {
		const values = [0.4, 0.5, 0.9];
		let call = 0;
		expect(nextIndex(3, 1, true, () => values[call++])).toBe(2);
		expect(call).toBe(3);
	});
	it('ignores shuffle with a single track', () => {
		expect(nextIndex(1, 0, true)).toBe(0);
	});
});

describe('previousIndex', () => {
	it('steps back and wraps around', () => {
		expect(previousIndex(3, 2)).toBe(1);
		expect(previousIndex(3, 0)).toBe(2);
	});
});
