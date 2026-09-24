<script lang="ts">
	import { albumCover } from '$lib/utils/hrefs';
	import { albumMeta } from '$lib/utils/format';
	import MediaCard from '$lib/components/ui/media/MediaCard.svelte';
	import MediaCreateCard from '$lib/components/ui/media/MediaCreateCard.svelte';

	let { data } = $props();
</script>

<div class="mt-4 grid-cards-lg">
	<MediaCreateCard title="Nuevo álbum" subtitle="Álbum, EP o Single" href="/library/albums/new" />
	{#each data.albums.items as album, i (album.id)}
		<MediaCard
			href="/library/albums/{album.id}"
			title={album.title}
			subtitle={albumMeta(album.releaseYear, album.trackCount)}
			trackIds={album.coverTrackIds}
			src={albumCover(album.id, 'large')}
			index={i + 1}
		/>
	{/each}
</div>
