export function appendUnique<T>(
	current: T[],
	incoming: T[],
	keyOf: (item: T) => string | number
): T[] {
	const seen = new Set(current.map(keyOf));
	const added: T[] = [];
	for (const item of incoming) {
		const key = keyOf(item);
		if (seen.has(key)) continue;
		seen.add(key);
		added.push(item);
	}
	return added.length > 0 ? [...current, ...added] : current;
}
