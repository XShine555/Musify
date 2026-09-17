import { player, toQueueItems, type ApiTrackLike, type QueueItem } from '$lib/player/player.svelte';

export type TrackTarget = { track: ApiTrackLike };

export function targetId(target: TrackTarget): string | number {
	return target.track.id;
}

export function targetTitle(target: TrackTarget): string {
	return target.track.title;
}

export function targetArtist(target: TrackTarget): string | undefined {
	return target.track.artist ?? undefined;
}

export function targetOwnerUserId(target: TrackTarget): string | number | undefined {
	return target.track.ownerUserId ?? undefined;
}

export function targetExplicit(target: TrackTarget): boolean {
	return target.track.isExplicit ?? false;
}

export function targetListensCount(target: TrackTarget): number | string | undefined {
	return target.track.listensCount;
}

export function isTargetCurrent(target: TrackTarget): boolean {
	return player.current.id === target.track.id;
}

export function queueItemForTarget(target: TrackTarget): QueueItem {
	return toQueueItems([target.track])[0];
}

export function targetForQueueItem(item: QueueItem): TrackTarget {
	return {
		track: {
			id: item.id,
			title: item.title,
			artist: item.artist,
			isExplicit: item.explicit,
			ownerUserId: item.ownerUserId,
			listensCount: item.listensCount
		}
	};
}
