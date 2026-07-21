<script lang="ts">
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

<a
	href="/mixes/{mix.id}"
	class="group animate-enter min-w-0 text-left"
	style="animation-delay:{index * 45}ms"
>
	<MixArt
		items={mix.items}
		hue={hueFor(mix.id)}
		class="aspect-square w-full rounded-art shadow-art transition group-hover:shadow-art-lg"
	/>
	<div class="mt-2.5 truncate text-fg">{mix.title}</div>
	<div class="mt-0.5 truncate text-sm text-fg-3">
		{count}
		{count === 1 ? 'canción' : 'canciones'} · Musify y YouTube Music
	</div>
</a>
