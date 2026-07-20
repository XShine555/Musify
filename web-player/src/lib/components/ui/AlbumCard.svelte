<script lang="ts">
	import PlaylistArt from './PlaylistArt.svelte';
	import { hueFor } from '$lib/theme/color';

	interface Props {
		id: string;
		title: string;
		releaseYear?: number;
		trackCount: number;
		trackIds: (string | number)[];
		index?: number;
	}

	let { id, title, releaseYear, trackCount, trackIds, index = 0 }: Props = $props();

	const meta = $derived(
		[releaseYear, `${trackCount} ${trackCount === 1 ? 'canción' : 'canciones'}`]
			.filter((part) => part !== undefined && part !== null)
			.join(' · ')
	);
</script>

<a
	href="/albums/{id}"
	class="group animate-enter min-w-0 text-left"
	style="animation-delay:{index * 45}ms"
>
	<PlaylistArt
		{trackIds}
		hue={hueFor(id)}
		class="aspect-square w-full rounded-art shadow-art transition group-hover:shadow-art-lg"
	/>
	<div class="mt-2.5 truncate text-fg">{title}</div>
	<div class="mt-0.5 truncate text-sm text-fg-3">{meta}</div>
</a>
