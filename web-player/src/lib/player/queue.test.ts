import { describe, expect, it } from 'vitest';
import { append, insertNext, isLastIndex, move, nextIndex, previousIndex } from './queue';

const t = (id: string) => ({ id });
const ids = (queue: { id: string | number }[]) => queue.map((item) => item.id);

describe('insertNext', () => {
	it('inserts right after the current track without mutating', () => {
		const queue = ['a', 'b', 'c'].map(t);
		expect(ids(insertNext(queue, 0, ['x', 'y'].map(t)))).toEqual(['a', 'x', 'y', 'b', 'c']);
		expect(ids(queue)).toEqual(['a', 'b', 'c']);
	});
	it('inserts at the start when there is no current track', () => {
		expect(ids(insertNext([t('a')], -1, [t('x')]))).toEqual(['x', 'a']);
	});
	it('moves tracks already queued instead of duplicating them', () => {
		const queue = ['a', 'b', 'c', 'd'].map(t);
		expect(ids(insertNext(queue, 1, [t('d'), t('a')]))).toEqual(['b', 'd', 'a', 'c']);
	});
	it('ignores the current track', () => {
		const queue = ['a', 'b'].map(t);
		expect(ids(insertNext(queue, 0, [t('a')]))).toEqual(['a', 'b']);
	});
});

describe('append', () => {
	it('adds new items at the end', () => {
		expect(ids(append([t('a')], ['b', 'c'].map(t)))).toEqual(['a', 'b', 'c']);
	});
	it('skips tracks already in the queue and repeated ones', () => {
		expect(ids(append(['a', 'b'].map(t), ['b', 'c', 'c'].map(t)))).toEqual(['a', 'b', 'c']);
	});
	it('returns the same queue when nothing is added', () => {
		const queue = [t('a')];
		expect(append(queue, [t('a')])).toBe(queue);
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

describe('isLastIndex', () => {
	it('detects the end of the queue', () => {
		expect(isLastIndex(3, 2)).toBe(true);
		expect(isLastIndex(3, 1)).toBe(false);
	});
});
