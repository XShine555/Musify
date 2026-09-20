import type { Action } from 'svelte/action';

export const clickOutside: Action<HTMLElement, () => void> = (node, fn) => {
	function onClick(event: MouseEvent) {
		if (!node.contains(event.target as Node)) fn();
	}
	document.addEventListener('click', onClick, true);

	return {
		update(next) {
			fn = next;
		},
		destroy() {
			document.removeEventListener('click', onClick, true);
		}
	};
};
