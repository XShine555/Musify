import type { Action } from 'svelte/action';

export const pressable: Action<HTMLElement, () => void> = (node, fn) => {
	function onClick() {
		fn();
	}
	function onKeydown(event: KeyboardEvent) {
		if (event.key !== 'Enter' && event.key !== ' ') return;
		event.preventDefault();
		fn();
	}

	node.addEventListener('click', onClick);
	node.addEventListener('keydown', onKeydown);

	return {
		update(next) {
			fn = next;
		},
		destroy() {
			node.removeEventListener('click', onClick);
			node.removeEventListener('keydown', onKeydown);
		}
	};
};
