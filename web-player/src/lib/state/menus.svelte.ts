import {
	contextMenuPosition,
	type MenuPosition
} from '$lib/components/ui/overlay/ContextMenu.svelte';
import { player, toQueueItem, type ApiTrackLike } from '../player/player.svelte';

export type TrackMenuState = { track: ApiTrackLike } & MenuPosition;

export function createTrackMenu() {
	let state = $state<TrackMenuState | null>(null);

	return {
		get state() {
			return state;
		},
		open(event: MouseEvent, track: ApiTrackLike) {
			state = { track, ...contextMenuPosition(event) };
		},
		close() {
			state = null;
		},
		playNext() {
			if (!state) return;
			player.playNext([toQueueItem(state.track)]);
			state = null;
		}
	};
}
