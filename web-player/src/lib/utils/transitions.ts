import { expoOut } from 'svelte/easing';
import { crossfade, type TransitionConfig } from 'svelte/transition';

export function pop(
	node: Element,
	{ duration = 200 }: { duration?: number } = {}
): TransitionConfig {
	return {
		duration,
		easing: expoOut,
		css: (t) =>
			`opacity: ${t}; transform: scale(${0.98 + 0.02 * t}) translateY(${0.25 * (1 - t)}rem);`
	};
}

export function collapse(
	node: Element,
	{ duration = 200 }: { duration?: number } = {}
): TransitionConfig {
	const height = node.getBoundingClientRect().height;
	const style = getComputedStyle(node);
	const paddingTop = parseFloat(style.paddingTop);
	const paddingBottom = parseFloat(style.paddingBottom);
	return {
		duration,
		easing: expoOut,
		css: (t) =>
			`overflow: hidden; opacity: ${t}; height: ${height * t}px; padding-top: ${paddingTop * t}px; padding-bottom: ${paddingBottom * t}px;`
	};
}

export function transferBetweenLists() {
	return crossfade({ duration: 200, easing: expoOut });
}
