import type { Paged } from '$lib/types';

export function createPagedList<T>(
	fetchPage: (pageNumber: number) => Promise<Paged<T>>,
	keyOf: (item: T) => string | number
) {
	let items = $state<T[]>([]);
	let pageNumber = 1;
	let hasMore = $state(false);
	let loading = $state(false);
	let error = $state(false);

	return {
		get items() {
			return items;
		},
		get hasMore() {
			return hasMore;
		},
		get loading() {
			return loading;
		},
		get error() {
			return error;
		},
		reset(initial: Paged<T> | null) {
			items = initial?.items ?? [];
			pageNumber = initial?.pageNumber ?? 1;
			hasMore = initial?.hasNextPage ?? false;
			loading = false;
			error = false;
		},
		async loadMore() {
			if (loading) return;
			loading = true;
			error = false;
			try {
				const next = await fetchPage(pageNumber + 1);
				const seen = new Set(items.map(keyOf));
				items = [...items, ...next.items.filter((item) => !seen.has(keyOf(item)))];
				pageNumber = next.pageNumber;
				hasMore = next.hasNextPage;
			} catch {
				error = true;
			} finally {
				loading = false;
			}
		}
	};
}

export type PagedList<T> = ReturnType<typeof createPagedList<T>>;
