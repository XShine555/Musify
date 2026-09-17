<script lang="ts">
	import MediaCard from './MediaCard.svelte';
	import PlaylistArt from './PlaylistArt.svelte';

	interface Props {
		id: string;
		name: string;
		trackCount: number;
		trackIds: (string | number)[];
		updatedAt: string;
		index?: number;
	}

	let { id, name, trackCount, trackIds, updatedAt, index = 0 }: Props = $props();

	const subtitle = $derived(`${trackCount} ${trackCount === 1 ? 'Canción' : 'Canciones'}`);
</script>

<MediaCard href="/playlists/{id}" title={name} {subtitle} {index}>
	{#snippet art(artClass)}
		<PlaylistArt playlistId={id} {trackIds} version={updatedAt} class={artClass} />
	{/snippet}
</MediaCard>
