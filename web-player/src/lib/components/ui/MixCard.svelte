<script lang="ts">
	import MediaCard from './MediaCard.svelte';
	import MixArt from './MixArt.svelte';
	import { hueFor } from '$lib/theme/color';
	import type { Mix } from '$lib/mixes';

	interface Props {
		mix: Mix;
		index?: number;
	}

	let { mix, index = 0 }: Props = $props();

	const count = $derived(Number(mix.itemCount));
</script>

<MediaCard
	href="/mixes/{mix.id}"
	title={mix.title}
	subtitle="{count} {count === 1 ? 'canción' : 'canciones'}"
	{index}
>
	{#snippet art(artClass)}
		<MixArt items={mix.items} hue={hueFor(mix.id)} class={artClass} />
	{/snippet}
</MediaCard>
