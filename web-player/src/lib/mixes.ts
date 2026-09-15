import type { components } from '$lib/api/schema';
import type { TrackTarget } from '$lib/tracks';

export type Mix = components['schemas']['MixApplicationResponse'];
export type MixItem = components['schemas']['MixItemApplicationResponse'];

export function mixItemKey(item: MixItem): string {
	return `mf:${item.trackId}`;
}

export function mixItemTarget(item: MixItem): TrackTarget {
	return {
		track: {
			id: item.trackId,
			title: item.title,
			artist: item.artist
		}
	};
}
