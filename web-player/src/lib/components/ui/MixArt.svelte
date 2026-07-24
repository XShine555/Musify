<script lang="ts">
	import Cover from './Cover.svelte';
	import { mixItemKey, type MixItem } from '$lib/mixes';

	interface Props {
		items: MixItem[];
		class?: string;
	}

	let { items, class: klass = '' }: Props = $props();

	const tiles = $derived(items.slice(0, 4));
</script>

{#if tiles.length >= 4}
	<div class="grid grid-cols-2 grid-rows-2 overflow-hidden {klass}">
		{#each tiles as item (mixItemKey(item))}
			<Cover
				trackId={item.trackId ?? item.videoId ?? ''}
				src={item.thumbnailUrl}
				size="small"
				class="h-full w-full"
			/>
		{/each}
	</div>
{:else if tiles.length > 0}
	<Cover
		trackId={tiles[0].trackId ?? tiles[0].videoId ?? ''}
		src={tiles[0].thumbnailUrl}
		size="medium"
		class={klass}
	/>
{:else}
	<div class="bg-surface {klass}"></div>
{/if}
