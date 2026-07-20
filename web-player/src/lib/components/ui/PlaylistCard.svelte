<script lang="ts">
	import PlaylistArt from './PlaylistArt.svelte';
	import { hueFor } from '$lib/theme/color';

	interface Props {
		id: string;
		name: string;
		description?: string | null;
		trackIds: (string | number)[];
		updatedAt: string;
		index?: number;
	}

	let { id, name, description, trackIds, updatedAt, index = 0 }: Props = $props();
</script>

<a
	href="/playlists/{id}"
	class="group animate-enter min-w-0 text-left"
	style="animation-delay:{index * 45}ms"
>
	<PlaylistArt
		playlistId={id}
		{trackIds}
		hue={hueFor(id)}
		version={updatedAt}
		class="aspect-square w-full rounded-art shadow-art transition group-hover:shadow-art-lg"
	/>
	<div class="mt-2.5 truncate text-fg">{name}</div>
	{#if description}
		<div class="mt-0.5 truncate text-sm text-fg-3">{description}</div>
	{/if}
</a>
