<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		as?: 'ul' | 'div';
		min?: string;
		class?: string;
		children: Snippet;
	}

	let { as = 'div', min = '180px', class: klass = '', children }: Props = $props();
</script>

<svelte:element
	this={as}
	class="grid gap-4 {klass}"
	style="grid-template-columns:repeat(auto-fill,minmax({min},1fr))"
>
	{@render children()}
</svelte:element>
