<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		eyebrow: string;
		title: string;
		description?: string;
		meta?: string;
		align?: 'center' | 'end';
		cover: Snippet;
		actions?: Snippet;
	}

	let { eyebrow, title, description, meta, align = 'end', cover, actions }: Props = $props();
</script>

<div
	class="flex flex-col gap-5 sm:flex-row sm:gap-6 {align === 'end'
		? 'mt-3 sm:mt-4 sm:items-end'
		: 'sm:items-center'}"
>
	{@render cover()}
	<div class="min-w-0 flex-1">
		<p class="text-xs font-medium tracking-widest text-fg-2 uppercase">{eyebrow}</p>
		<h1
			class="{align === 'end'
				? 'mt-1.5'
				: 'mt-3'} font-display text-3xl font-medium tracking-tight wrap-break-word text-fg sm:text-4xl"
		>
			{title}
		</h1>
		{#if description}
			<p class="mt-3 max-w-2xl text-sm text-fg-2 sm:text-base">{description}</p>
		{/if}
		{#if meta}
			<p class="mt-3 text-sm text-fg-2">{meta}</p>
		{/if}
	</div>
</div>

{#if actions}
	<div class="mt-6 flex flex-wrap items-center gap-2.5 sm:mt-7">
		{@render actions()}
	</div>
{/if}
