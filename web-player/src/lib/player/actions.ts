import { shuffle } from '$lib/utils/collections';
import type { Track } from '$lib/types';
import { player } from './player.svelte';

export function isQueueCurrent(items: { id: string }[]): boolean {
	return items.some((item) => item.id === player.currentId);
}

export function playAllOrToggle(items: Track[]) {
	if (items.length === 0) return;
	if (isQueueCurrent(items)) player.toggle();
	else player.playQueue(items, 0);
}

export function playShuffled(items: Track[]) {
	if (items.length === 0) return;
	player.playQueue(shuffle(items), 0);
}
