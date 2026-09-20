<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		title: string;
		eyebrow?: string;
		description?: string;
		meta?: string;
		cover?: Snippet;
		actions?: Snippet;
		align?: 'center' | 'end';
		class?: string;
	}

	let {
		title,
		eyebrow,
		description,
		meta,
		cover,
		actions,
		align = 'end',
		class: klass = ''
	}: Props = $props();
</script>

<div class="mb-7 sm:mb-8 {klass}">
	<div
		class="flex flex-col gap-5 sm:flex-row sm:gap-6 {cover
			? align === 'end'
				? 'sm:items-end'
				: 'sm:items-center'
			: 'sm:items-end sm:justify-between'}"
	>
		{#if cover}
			{@render cover()}
		{/if}
		<div class="min-w-0 flex-1">
			{#if eyebrow}
				<p class="text-eyebrow text-fg-2">{eyebrow}</p>
			{/if}
			<h1 class="text-display-1 wrap-break-word text-fg {eyebrow ? 'mt-1.5' : ''}">
				{title}
			</h1>
			{#if description}
				<p class="mt-3 max-w-2xl text-body text-fg-2">{description}</p>
			{/if}
			{#if meta}
				<p class="mt-3 text-sm text-fg-2">{meta}</p>
			{/if}
		</div>
		{#if actions && !cover}
			<div class="flex shrink-0 items-center gap-3">{@render actions()}</div>
		{/if}
	</div>
	{#if actions && cover}
		<div class="mt-6 flex flex-wrap items-center gap-2.5 sm:mt-7">
			{@render actions()}
		</div>
	{/if}
</div>
