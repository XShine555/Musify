<script lang="ts">
	import MediaCard from './MediaCard.svelte';
	import Artwork from './Artwork.svelte';
	import { plural } from '$lib/format';

	interface Props {
		id: string;
		name: string;
		trackCount: number;
		trackIds: (string | number)[];
		updatedAt: string;
		index?: number;
	}

	let { id, name, trackCount, trackIds, updatedAt, index = 0 }: Props = $props();

	const subtitle = $derived(plural(trackCount, 'canción', 'canciones'));
</script>

<MediaCard href="/playlists/{id}" title={name} {subtitle} {index}>
	{#snippet art(artClass)}
		<Artwork
			src="/api/playlists/{id}/cover?size=large&v={encodeURIComponent(updatedAt)}"
			{trackIds}
			size="fill"
			alt={name}
			class={artClass}
		/>
	{/snippet}
</MediaCard>
