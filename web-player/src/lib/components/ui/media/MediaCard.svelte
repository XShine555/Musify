<script lang="ts">
	import type { Snippet } from 'svelte';
	import Artwork from './Artwork.svelte';
	import PlayButton from './PlayButton.svelte';

	interface Props {
		href: string;
		title: string;
		subtitle?: string | null;
		src?: string | null;
		trackIds?: (string | number)[];
		onPlay?: (event: MouseEvent) => void;
		art?: Snippet;
		oncontextmenu?: (event: MouseEvent) => void;
		index?: number;
	}

	let {
		href,
		title,
		subtitle,
		src,
		trackIds = [],
		onPlay,
		art,
		oncontextmenu,
		index = 0
	}: Props = $props();
</script>

<a {href} class="group animate-enter min-w-0 text-left" style="--i:{index}" {oncontextmenu}>
	<div class="relative">
		{#if art}
			{@render art()}
		{:else}
			<Artwork
				{src}
				{trackIds}
				size="fill"
				alt={title}
				class="rounded-art transition duration-300 ease-snappy group-hover:scale-105"
			/>
		{/if}
		{#if onPlay}
			<div
				class="pointer-events-none absolute inset-0 rounded-art bg-linear-to-b from-transparent from-45% to-scrim/40"
			></div>
			<PlayButton
				variant="glass"
				size="sm"
				onclick={onPlay}
				label="Reproducir {title}"
				class="absolute right-2.5 bottom-2.5 opacity-0 transition-opacity group-hover:opacity-100"
			/>
		{/if}
	</div>
	<div class="mt-2.5 truncate text-sm text-fg">{title}</div>
	{#if subtitle}
		<div class="mt-0.5 truncate text-xs text-fg-2">{subtitle}</div>
	{/if}
</a>
