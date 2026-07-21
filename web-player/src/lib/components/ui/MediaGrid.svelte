<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		as?: 'ul' | 'div';
		min?: string;
		minMobile?: string;
		class?: string;
		children: Snippet;
	}

	let {
		as = 'div',
		min = '165px',
		minMobile = '150px',
		class: klass = '',
		children
	}: Props = $props();
</script>

<svelte:element
	this={as}
	class="grid media-grid gap-3 sm:gap-4 {klass}"
	style="--mf-grid-min:{min}; --mf-grid-min-mobile:{minMobile}"
>
	{@render children()}
</svelte:element>
