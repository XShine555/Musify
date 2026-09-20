import {
	contextMenuStateFor,
	type ContextMenuState
} from '$lib/components/TrackContextMenu.svelte';
import { player } from './player/player.svelte';
import { queueItemForTarget, type TrackTarget } from './tracks';

export function createTrackMenu() {
	let state = $state<ContextMenuState | null>(null);

	return {
		get state() {
			return state;
		},
		open(event: MouseEvent, target: TrackTarget) {
			state = contextMenuStateFor(event, target);
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
