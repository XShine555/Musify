import { MENU_WIDTH, SUBMENU_WIDTH } from '$lib/config';

export interface MenuPosition {
	x: number;
	y: number;
	openLeft: boolean;
}

export function contextMenuPosition(event: MouseEvent): MenuPosition {
	return {
		x: Math.max(8, Math.min(event.clientX, window.innerWidth - MENU_WIDTH - 8)),
		y: Math.max(8, Math.min(event.clientY, window.innerHeight - 60)),
		openLeft: event.clientX + MENU_WIDTH + SUBMENU_WIDTH + 16 > window.innerWidth
	};
}
