import {
	player,
	queueIdForTrack,
	toQueueItems,
	type ApiTrackLike,
	type QueueItem
} from '$lib/player/player.svelte';
import type { YouTubeSong } from '$lib/types';

export type TrackTarget =
	{ kind: 'youtube'; song: YouTubeSong } | { kind: 'local'; track: ApiTrackLike };

export function targetId(target: TrackTarget): string | number {
	return target.kind === 'youtube' ? target.song.videoId : target.track.id;
}

export function targetTitle(target: TrackTarget): string {
	return target.kind === 'youtube' ? target.song.title : target.track.title;
}

export function targetArtist(target: TrackTarget): string | undefined {
	return (target.kind === 'youtube' ? target.song.artist : target.track.artist) ?? undefined;
}

export function targetOwnerUserId(target: TrackTarget): string | number | undefined {
	return target.kind === 'youtube' ? undefined : (target.track.ownerUserId ?? undefined);
}

export function targetExplicit(target: TrackTarget): boolean {
	return target.kind === 'youtube' ? target.song.isExplicit : (target.track.isExplicit ?? false);
}

export function targetDurationSeconds(target: TrackTarget): number | undefined {
	return target.kind === 'youtube' ? target.song.durationSeconds : undefined;
}

export function targetCoverSrc(target: TrackTarget): string | undefined {
	return target.kind === 'youtube' ? target.song.thumbnailUrl : undefined;
}

function queueIdForTarget(target: TrackTarget): string | number {
	return target.kind === 'youtube' ? target.song.videoId : queueIdForTrack(target.track);
}

export function isTargetCurrent(target: TrackTarget): boolean {
	return player.current.id === queueIdForTarget(target);
}

export function queueItemForTarget(target: TrackTarget): QueueItem {
	return target.kind === 'youtube'
		? {
				id: target.song.videoId,
				title: target.song.title,
				artist: target.song.artist,
				source: 'youtube',
				coverUrl: target.song.thumbnailUrl,
				explicit: target.song.isExplicit
			}
		: toQueueItems([target.track])[0];
}

export function targetForQueueItem(item: QueueItem): TrackTarget {
	return item.source === 'youtube'
		? {
				kind: 'youtube',
				song: {
					videoId: String(item.id),
					title: item.title,
					artist: item.artist ?? '',
					album: '',
					durationSeconds: 0,
					thumbnailUrl: item.coverUrl ?? '',
					isExplicit: item.explicit ?? false
				}
			}
		: {
				kind: 'local',
				track: {
					id: item.id,
					title: item.title,
					artist: item.artist,
					source: 'Local',
					isExplicit: item.explicit,
					ownerUserId: item.ownerUserId
				}
			};
}
