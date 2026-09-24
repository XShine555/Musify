export function insertNext<T>(queue: T[], afterIndex: number, items: T[]): T[] {
	const result = [...queue];
	result.splice(afterIndex + 1, 0, ...items);
	return result;
}

export function append<T>(queue: T[], items: T[]): T[] {
	return [...queue, ...items];
}

export function move<T>(queue: T[], from: number, insertAt: number): T[] {
	if (from < 0 || from >= queue.length) return queue;
	const target = insertAt > from ? insertAt - 1 : insertAt;
	if (target === from) return queue;
	const result = [...queue];
	const [item] = result.splice(from, 1);
	result.splice(target, 0, item);
	return result;
}

export function nextIndex(
	length: number,
	index: number,
	shuffle: boolean,
	random: () => number = Math.random
): number {
	if (shuffle && length > 1) {
		let next: number;
		do {
			next = Math.floor(random() * length);
		} while (next === index);
		return next;
	}
	return (index + 1) % length;
}

export function previousIndex(length: number, index: number): number {
	return (index - 1 + length) % length;
}
