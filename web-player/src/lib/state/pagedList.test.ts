import { describe, expect, it, vi } from 'vitest';
import type { Paged } from '$lib/types';
import { createPagedList } from './pagedList.svelte';

interface Item {
	id: number;
}

function page(ids: number[], pageNumber: number, hasNextPage: boolean): Paged<Item> {
	return { items: ids.map((id) => ({ id })), pageNumber, hasNextPage, totalItemCount: 99 };
}

describe('createPagedList', () => {
	it('appends the next page without duplicates and updates hasMore', async () => {
		const fetchPage = vi.fn().mockResolvedValue(page([2, 3], 2, false));
		const list = createPagedList<Item>(fetchPage, (item) => item.id);
		list.reset(page([1, 2], 1, true));
		expect(list.hasMore).toBe(true);

		await list.loadMore();

		expect(fetchPage).toHaveBeenCalledWith(2);
		expect(list.items.map((item) => item.id)).toEqual([1, 2, 3]);
		expect(list.hasMore).toBe(false);
	});

	it('ignores loadMore while another is in flight', async () => {
		let resolve: (value: Paged<Item>) => void = () => {};
		const fetchPage = vi.fn().mockImplementation(
			() =>
				new Promise<Paged<Item>>((r) => {
					resolve = r;
				})
		);
		const list = createPagedList<Item>(fetchPage, (item) => item.id);
		list.reset(page([1], 1, true));

		const first = list.loadMore();
		expect(list.loading).toBe(true);
		await list.loadMore();
		expect(fetchPage).toHaveBeenCalledTimes(1);

		resolve(page([2], 2, false));
		await first;
		expect(list.loading).toBe(false);
	});

	it('flags an error and keeps the items when the fetch fails', async () => {
		const fetchPage = vi.fn().mockRejectedValue(new Error('boom'));
		const list = createPagedList<Item>(fetchPage, (item) => item.id);
		list.reset(page([1, 2], 1, true));

		await list.loadMore();

		expect(list.error).toBe(true);
		expect(list.loading).toBe(false);
		expect(list.items).toHaveLength(2);
	});

	it('reset clears the error', async () => {
		const fetchPage = vi.fn().mockRejectedValue(new Error('boom'));
		const list = createPagedList<Item>(fetchPage, (item) => item.id);
		list.reset(page([1], 1, true));
		await list.loadMore();
		expect(list.error).toBe(true);

		list.reset(page([5], 1, false));

		expect(list.error).toBe(false);
		expect(list.items).toEqual([{ id: 5 }]);
		expect(list.hasMore).toBe(false);
	});
});
