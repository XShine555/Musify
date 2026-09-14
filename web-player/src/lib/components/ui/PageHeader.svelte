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
		<h1 class="font-display text-[28px] font-semibold tracking-[-0.03em] text-fg sm:text-[32px]">
			{title}
		</h1>
		{#if subtitle}
			<p class="mt-2 text-[13px] text-fg-2">{subtitle}</p>
		{/if}
	</div>
	{#if actions}
		<div class="flex shrink-0 items-center gap-3">{@render actions()}</div>
	{/if}
</div>
