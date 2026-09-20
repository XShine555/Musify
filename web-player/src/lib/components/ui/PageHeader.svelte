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

<div class="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between {klass}">
	<div class="min-w-0">
		<h1 class="font-display text-3xl font-medium tracking-tight text-fg sm:text-4xl">
			{title}
		</h1>
		{#if subtitle}
			<p class="mt-2 text-sm text-fg-2">{subtitle}</p>
		{/if}
	</div>
	{#if actions}
		<div class="flex shrink-0 items-center gap-3">{@render actions()}</div>
	{/if}
</div>
