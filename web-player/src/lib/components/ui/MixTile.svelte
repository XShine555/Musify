<script lang="ts">
	import Artwork from './Artwork.svelte';
	import PlayButton from './PlayButton.svelte';
	import { player } from '$lib/player/player.svelte';
	import { queueItemForTarget } from '$lib/tracks';
	import { mixItemTarget, type Mix } from '$lib/mixes';

	interface Props {
		mix: Mix;
		index?: number;
		class?: string;
	}

	let { mix, index = 0, class: klass = '' }: Props = $props();

	const trackIds = $derived(mix.items.map((item) => item.trackId));

	function play(event: MouseEvent) {
		event.preventDefault();
		const items = mix.items.map((item) => queueItemForTarget(mixItemTarget(item)));
		player.playQueue(items, 0);
	}
</script>

<a
	href="/mixes/{mix.id}"
	class="group/tile animate-enter block cursor-pointer {klass}"
	style="--i:{index}"
>
	<Artwork
		{trackIds}
		size="fill"
		alt={mix.title}
		class="rounded-art transition duration-300 group-hover/tile:scale-105"
	>
		<div
			class="pointer-events-none absolute inset-0 bg-linear-to-b from-transparent from-45% to-scrim/40"
		></div>
		<PlayButton
			variant="glass"
			size="sm"
			onclick={play}
			label="Reproducir {mix.title}"
			class="absolute right-2.5 bottom-2.5 opacity-0 transition-opacity duration-150 group-hover/tile:opacity-100"
		/>
	</Artwork>
	<div class="mt-3.25 truncate text-sm font-semibold text-fg">
		{mix.title}
	</div>
	{#if mix.subtitle}
		<div class="mt-1 truncate text-xs text-fg-3">{mix.subtitle}</div>
	{/if}
</a>
