<script lang="ts">
	import type { Snippet } from 'svelte';
	import { plural } from '$lib/format';

	interface Props {
		title: string;
		subtitle?: string;
		count?: number;
		href?: string;
		linkLabel?: string;
		class?: string;
		actions?: Snippet;
	}

	let {
		title,
		subtitle,
		count,
		href,
		linkLabel = 'Ver todas',
		class: klass = '',
		actions
	}: Props = $props();

	const hasActions = $derived(!!actions || count !== undefined || !!href);
</script>

{#snippet heading()}
	<h2 class="text-display-4 text-fg">
		{title}
	</h2>
	{#if subtitle}
		<p class="mt-1.25 text-xs text-fg-3">{subtitle}</p>
	{/if}
{/snippet}

{#snippet autoActions()}
	{#if count !== undefined}
		<div class="h-px flex-1 self-center bg-line"></div>
		<span class="shrink-0 text-xs text-muted tabular-nums">
			{plural(count, 'resultado', 'resultados')}
		</span>
	{/if}
	{#if href}
		<a
			{href}
			class="shrink-0 text-xs font-medium tracking-wider text-fg-3 uppercase transition-colors hover:text-fg"
		>
			{linkLabel}
		</a>
	{/if}
{/snippet}

{#if hasActions}
	<div class="mb-4 flex items-end justify-between gap-4 sm:mb-4.5 {klass}">
		<div>{@render heading()}</div>
		{#if actions}
			{@render actions()}
		{:else}
			{@render autoActions()}
		{/if}
	</div>
{:else}
	<div class="mb-4 sm:mb-4.5 {klass}">
		{@render heading()}
	</div>
{/if}
