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
	<div class="mb-4 flex items-center justify-between gap-4 sm:mb-6 {klass}">
		<h2 class="text-xl font-bold text-fg sm:text-2xl">{title}</h2>
		{@render actions()}
	</div>
{:else}
	<h2 class="mb-4 text-xl font-bold text-fg sm:mb-6 sm:text-2xl {klass}">{title}</h2>
{/if}
