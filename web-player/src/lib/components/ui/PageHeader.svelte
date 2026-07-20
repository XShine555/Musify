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

<div class="flex items-end justify-between gap-4 {klass}">
	<div class="min-w-0">
		<h1 class="text-3xl font-bold tracking-tight text-fg sm:text-4xl">{title}</h1>
		{#if subtitle}
			<p class="mt-2 text-fg-2">{subtitle}</p>
		{/if}
	</div>
	{#if actions}
		<div class="flex shrink-0 items-center gap-3">{@render actions()}</div>
	{/if}
</div>
