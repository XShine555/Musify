import { afterNavigate, beforeNavigate } from '$app/navigation';
import { historyIndex } from './history.svelte';

const scrollPositions = new Map<number, number>();

export function restoreScroll(getScroller: () => HTMLElement | undefined) {
	let current: number | null = null;

	beforeNavigate(() => {
		const scroller = getScroller();
		if (current !== null && scroller) scrollPositions.set(current, scroller.scrollTop);
	});

	afterNavigate((nav) => {
		current = historyIndex();
		const scroller = getScroller();
		if (!scroller || nav.type === 'enter' || nav.to?.url.hash) return;
		const saved =
			nav.type === 'popstate' && current !== null ? scrollPositions.get(current) : undefined;
		scroller.scrollTop = saved ?? 0;
	});
}
