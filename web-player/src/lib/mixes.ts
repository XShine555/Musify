import type { components } from '$lib/api/schema';
import type { TrackTarget } from '$lib/tracks';

export type Mix = components['schemas']['MixApplicationResponse'];
export type MixItem = components['schemas']['MixItemApplicationResponse'];

export function mixItemKey(item: MixItem): string {
	return item.source === 'YouTube' ? `yt:${item.videoId}` : `mf:${item.trackId}`;
}

export function mixItemTarget(item: MixItem): TrackTarget {
	if (item.source === 'YouTube') {
		return {
			kind: 'youtube',
			song: {
				videoId: item.videoId ?? '',
				title: item.title,
				artist: item.artist ?? '',
				album: '',
				durationSeconds: Number(item.durationSeconds),
				thumbnailUrl: item.thumbnailUrl ?? '',
				isExplicit: item.isExplicit
			}
		};
	}

	return {
		kind: 'local',
		track: {
			id: item.trackId ?? '',
			title: item.title,
			artist: item.artist,
			source: 'Local',
			isExplicit: item.isExplicit
		}
	};
}
