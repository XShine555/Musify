interface Identified {
	id: string;
}

export function insertNext<T extends Identified>(
	queue: T[],
	currentIndex: number,
	items: T[]
): T[] {
	const currentId = queue[currentIndex]?.id;
	const incoming = items.filter((item) => item.id !== currentId);
	const moving = new Set(incoming.map((item) => item.id));
	const rest = queue.filter((item) => !moving.has(item.id));
	const anchor = currentId === undefined ? -1 : rest.findIndex((item) => item.id === currentId);
	return [...rest.slice(0, anchor + 1), ...incoming, ...rest.slice(anchor + 1)];
}

export function append<T extends Identified>(queue: T[], items: T[]): T[] {
	const present = new Set(queue.map((item) => item.id));
	const added = items.filter((item) => {
		if (present.has(item.id)) return false;
		present.add(item.id);
		return true;
	});
	return added.length > 0 ? [...queue, ...added] : queue;
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

export function isLastIndex(length: number, index: number): boolean {
	return index >= length - 1;
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
