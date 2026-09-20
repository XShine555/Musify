import { player, toQueueItems, type ApiTrackLike, type QueueItem } from '$lib/player/player.svelte';

/**
 * Merges the in-memory session history (tracks played since the page loaded)
 * with the server-side listening history, de-duplicated by id and capped at `limit`.
 */
export function mergeRecentlyPlayed(history: ApiTrackLike[], limit: number): QueueItem[] {
	const seen = new Set<string>();
	const merged: QueueItem[] = [];

	for (const t of player.recentlyPlayed) {
		const id = String(t.id);
		if (seen.has(id)) continue;
		seen.add(id);
		merged.push({ ...t, id });
	}

	for (const item of toQueueItems(history)) {
		const id = String(item.id);
		if (seen.has(id)) continue;
		seen.add(id);
		merged.push({ ...item, id });
	}

	return merged.slice(0, limit);
}
