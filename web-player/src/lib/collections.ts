export function shuffle<T>(items: T[]): T[] {
	const copy = [...items];
	for (let i = copy.length - 1; i > 0; i--) {
		const j = Math.floor(Math.random() * (i + 1));
		[copy[i], copy[j]] = [copy[j], copy[i]];
	}
	return copy;
}

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
