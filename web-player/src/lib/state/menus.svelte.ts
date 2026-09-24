import { contextMenuPosition, type MenuPosition } from '$lib/utils/menuPosition';
import { player } from '$lib/player/player.svelte';
import type { Track } from '$lib/types';

export type TrackMenuState = { track: Track } & MenuPosition;

export function createTrackMenu() {
	let state = $state<TrackMenuState | null>(null);

	return {
		get state() {
			return state;
		},
		open(event: MouseEvent, track: Track) {
			event.preventDefault();
			state = { track, ...contextMenuPosition(event) };
		},
		close() {
			state = null;
		},
		playNext() {
			if (!state) return;
			player.playNext([state.track]);
			state = null;
		}
	};
}
