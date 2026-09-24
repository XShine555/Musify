<script lang="ts">
	import type { Album, LibraryTrack } from '$lib/types';
	import { albumCover } from '$lib/utils/hrefs';
	import { albumMeta } from '$lib/utils/format';
	import MediaCard from '$lib/components/ui/media/MediaCard.svelte';
	import MediaCreateCard from '$lib/components/ui/media/MediaCreateCard.svelte';
	import LibraryAlbumEditor from './LibraryAlbumEditor.svelte';

	interface Props {
		albums: Album[];
		tracks: LibraryTrack[];
		albumTrackIds: Record<string, string[]>;
		formMessage?: string;
	}

	let { albums, tracks, albumTrackIds, formMessage }: Props = $props();

	let editing = $state<{ album: Album | null } | null>(null);
</script>

{#if editing}
	<div class="mt-4">
		{#key editing.album?.id}
			<LibraryAlbumEditor
				album={editing.album}
				{tracks}
				memberIds={editing.album ? (albumTrackIds[editing.album.id] ?? []) : []}
				{formMessage}
				onClose={() => (editing = null)}
			/>
		{/key}
	</div>
{:else}
	<div class="mt-4 grid-cards-lg">
		<MediaCreateCard
			title="Nuevo álbum"
			subtitle="Álbum, EP o Single"
			onclick={() => (editing = { album: null })}
		/>
		{#each albums as album, i (album.id)}
			<MediaCard
				href="/albums/{album.id}"
				title={album.title}
				subtitle={albumMeta(album.releaseYear, album.trackCount)}
				trackIds={album.coverTrackIds}
				src={albumCover(album.id, 'large')}
				index={i + 1}
				onclick={(event) => {
					event.preventDefault();
					editing = { album };
				}}
			/>
		{/each}
	</div>
{/if}
