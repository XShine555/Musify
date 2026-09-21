import type { components } from '$lib/api/schema';
import type { ApiTrackLike } from '$lib/player/player.svelte';

export type Mix = components['schemas']['MixApplicationResponse'];
export type MixItem = components['schemas']['MixItemApplicationResponse'];

export function mixItemKey(item: MixItem): string {
	return `mf:${item.trackId}`;
}

export function mixItemTrack(item: MixItem): ApiTrackLike {
	return {
		id: item.trackId,
		title: item.title,
		artist: item.artist,
		listensCount: item.listensCount
	};
}
