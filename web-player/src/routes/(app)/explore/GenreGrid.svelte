<script lang="ts">
	import Music from '@lucide/svelte/icons/music';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import { genreHref } from '$lib/utils/hrefs';
	import type { genreTiles } from '$lib/data/genres';

	interface Props {
		tiles: ReturnType<typeof genreTiles>;
	}

	let { tiles }: Props = $props();
</script>

<div class="grid-wide">
	{#each tiles as tile, i (tile.genre)}
		<a
			href={genreHref(tile.genre)}
			class="animate-enter group relative flex h-28 flex-col gap-1 overflow-hidden rounded-panel p-4 transition-[filter] genre-tile hover:brightness-110"
			style="--i:{i}; --tile-hue:{tile.hue}"
		>
			<div class="text-on-art">
				{tile.label}
			</div>
			<div class="text-xs text-on-art-2">{tile.tagline}</div>
		</a>
	{/each}
</div>
{#if tiles.length === 0}
	<EmptyState icon={Music} description="Todavía no hay géneros. Sube música para empezar." />
{/if}
