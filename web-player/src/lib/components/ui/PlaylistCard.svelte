<script lang="ts">
	import MediaCard from './MediaCard.svelte';
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

<MediaCard href="/playlists/{id}" title={name} subtitle={description} {index}>
	{#snippet art(artClass)}
		<PlaylistArt playlistId={id} {trackIds} hue={hueFor(id)} version={updatedAt} class={artClass} />
	{/snippet}
</MediaCard>
