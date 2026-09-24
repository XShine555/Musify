import type { Action } from 'svelte/action';

export const onEscape: Action<HTMLElement, (() => void) | undefined> = (_node, handler) => {
	function onKeydown(event: KeyboardEvent) {
		if (event.key === 'Escape') handler?.();
	}
	window.addEventListener('keydown', onKeydown);

	return {
		update(next) {
			handler = next;
		},
		destroy() {
			window.removeEventListener('keydown', onKeydown);
		}
	};
};
