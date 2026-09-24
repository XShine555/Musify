import { contextMenuPosition, type MenuPosition } from '$lib/utils/menuPosition';

export type MenuState<T> = { target: T } & MenuPosition;

export function createMenu<T>() {
	let state = $state<MenuState<T> | null>(null);

	return {
		get state() {
			return state;
		},
		open(event: MouseEvent, target: T) {
			event.preventDefault();
			state = { target, ...contextMenuPosition(event) };
		},
		close() {
			state = null;
		}
	};
}

export type Menu<T> = ReturnType<typeof createMenu<T>>;
