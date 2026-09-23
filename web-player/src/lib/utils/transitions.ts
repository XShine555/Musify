import { expoOut } from 'svelte/easing';
import type { TransitionConfig } from 'svelte/transition';

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
