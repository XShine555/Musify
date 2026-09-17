<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		title: string;
		subtitle?: string;
		class?: string;
		actions?: Snippet;
	}

	let { title, subtitle, class: klass = '', actions }: Props = $props();
</script>

{#snippet heading()}
	<h2 class="font-display text-lg font-medium tracking-tight text-fg">
		{title}
	</h2>
	{#if subtitle}
		<p class="mt-1.25 text-xs text-fg-3">{subtitle}</p>
	{/if}
{/snippet}

{#if actions}
	<div class="mb-4 flex items-end justify-between gap-4 sm:mb-4.5 {klass}">
		<div>{@render heading()}</div>
		{@render actions()}
	</div>
{:else}
	<div class="mb-4 sm:mb-4.5 {klass}">
		{@render heading()}
	</div>
{/if}
