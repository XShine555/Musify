import type { components } from '$lib/api/schema';
import type { ApiTrackLike } from '$lib/player/player.svelte';

export type Mix = components['schemas']['MixApplicationResponse'];
export type MixItem = components['schemas']['MixItemApplicationResponse'];

const MIX_TEXTS: Record<Mix['kind'], { title: string; subtitle: string }> = {
	Discovery: {
		title: 'Descubrimiento',
		subtitle: 'Canciones de tu biblioteca que todavía no has escuchado.'
	},
	Daily: {
		title: 'Tu mezcla diaria',
		subtitle: 'Lo que más escuchas, con alguna sorpresa de tu biblioteca.'
	}
};

export function mixText(mix: Pick<Mix, 'kind'>): { title: string; subtitle: string } {
	return MIX_TEXTS[mix.kind];
}

export function mixItemKey(item: MixItem): string {
	return `mf:${item.trackId}`;
}

export function mixItemTrack(item: MixItem): ApiTrackLike {
	return {
		id: item.trackId,
		title: item.title,
		artist: item.artist,
		duration: item.durationSeconds,
		listensCount: item.listensCount
	};
}
