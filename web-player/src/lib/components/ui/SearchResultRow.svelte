<script lang="ts" module>
	export type SearchResultKind = 'track' | 'album' | 'playlist' | 'user';
</script>

<script lang="ts">
	import type { Snippet } from 'svelte';
	import ExplicitBadge from './ExplicitBadge.svelte';

	const RADIUS: Record<SearchResultKind, string> = {
		track: 'rounded-control',
		album: 'rounded-[6px]',
		playlist: 'rounded-[13px]',
		user: 'rounded-full'
	};

	interface Props {
		kind: SearchResultKind;
		title: string;
		subtitle?: string | null;
		meta?: string;
		explicit?: boolean;
		href?: string;
		onclick?: () => void;
		oncontextmenu?: (event: MouseEvent) => void;
		art: Snippet<[string]>;
	}

	let {
		kind,
		title,
		subtitle,
		meta,
		explicit = false,
		href,
		onclick,
		oncontextmenu,
		art
	}: Props = $props();

	const artClass = $derived(
		`relative h-13 w-13 shrink-0 overflow-hidden shadow-art ${RADIUS[kind]}`
	);
</script>

{#snippet content()}
	{@render art(artClass)}
	<div class="min-w-0 flex-1">
		<div class="flex min-w-0 items-center gap-1.75">
			{#if explicit}
				<ExplicitBadge />
			{/if}
			<span class="truncate text-sm font-medium text-fg">{title}</span>
		</div>
		{#if subtitle}
			<div class="mt-0.75 truncate text-xs text-fg-3">{subtitle}</div>
		{/if}
	</div>
	{#if meta}
		<span class="shrink-0 text-xs text-muted tabular-nums">{meta}</span>
	{/if}
{/snippet}

{#if href}
	<a
		{href}
		{oncontextmenu}
		class="group/row flex w-full min-w-0 items-center gap-3.5 rounded-control p-2 pr-4 text-left transition hover:bg-surface"
	>
		{@render content()}
	</a>
{:else}
	<button
		type="button"
		{onclick}
		{oncontextmenu}
		class="group/row flex w-full min-w-0 items-center gap-3.5 rounded-control p-2 pr-4 text-left transition hover:bg-surface focus-visible:ring-2 focus-visible:ring-accent focus-visible:outline-none"
	>
		{@render content()}
	</button>
{/if}
