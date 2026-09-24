/** Largest page size the API accepts (`PageRequest.MaxPageSize` in the backend). */
export const API_MAX_PAGE_SIZE = 100;

type Page<T> = { items: T[]; hasNextPage: boolean };

/** Loads consecutive pages until the list ends or `maxItems` items were collected. */
export async function fetchAllPages<T>(
	loadPage: (pageNumber: number, pageSize: number) => Promise<Page<T> | undefined>,
	maxItems: number
): Promise<T[]> {
	const items: T[] = [];

	for (let pageNumber = 1; items.length < maxItems; pageNumber++) {
		const page = await loadPage(pageNumber, API_MAX_PAGE_SIZE);
		if (!page) break;

		items.push(...page.items);
		if (!page.hasNextPage) break;
	}

	return items.slice(0, maxItems);
}
