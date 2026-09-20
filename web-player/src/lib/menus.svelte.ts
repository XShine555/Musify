import { contextMenuPosition, type MenuPosition } from '$lib/components/ui/ContextMenu.svelte';
import { player } from './player/player.svelte';
import { queueItemForTarget, type TrackTarget } from './tracks';

export type TrackMenuState = TrackTarget & MenuPosition;

export function createTrackMenu() {
	let state = $state<TrackMenuState | null>(null);

	return {
		get state() {
			return state;
		},
		open(event: MouseEvent, target: TrackTarget) {
			state = { ...target, ...contextMenuPosition(event) };
		},
		close() {
			state = null;
		},
		playNext() {
			if (!state) return;
			player.playNextItem(queueItemForTarget(state));
			state = null;
		}
	};
}
