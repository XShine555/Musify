<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		title: string;
		class?: string;
		actions?: Snippet;
	}

	let { title, class: klass = '', actions }: Props = $props();
</script>

{#if actions}
	<div class="mb-3 flex items-baseline justify-between gap-4 sm:mb-4 {klass}">
		<h2 class="text-lg font-semibold tracking-tight text-fg sm:text-xl">{title}</h2>
		{@render actions()}
	</div>
{:else}
	<h2 class="mb-3 text-lg font-semibold tracking-tight text-fg sm:mb-4 sm:text-xl {klass}">
		{title}
	</h2>
{/if}
