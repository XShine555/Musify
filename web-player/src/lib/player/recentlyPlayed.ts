import { player } from '$lib/player/player.svelte';
import type { Track } from '$lib/types';

export function mergeRecentlyPlayed(history: Track[], limit: number): Track[] {
	const seen = new Set<string>();
	const merged: Track[] = [];

	for (const track of [...player.recentlyPlayed, ...history]) {
		if (seen.has(track.id)) continue;
		seen.add(track.id);
		merged.push(track);
	}

	return merged.slice(0, limit);
}
