<script lang="ts">
	import ListMusic from '@lucide/svelte/icons/list-music';
	import Page from '$lib/components/ui/Page.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import MediaGrid from '$lib/components/ui/MediaGrid.svelte';
	import PlaylistCard from '$lib/components/ui/PlaylistCard.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { createPlaylistModal } from '$lib/playlists.svelte';
	import { fmtDurationLong, plural } from '$lib/format';

	let { data } = $props();

	const items = $derived(data.playlists.items);
	const totals = $derived(data.totals);
	const summary = $derived(
		`${plural(totals.playlistCount, 'playlist', 'playlists')} · ${plural(totals.trackCount, 'canción', 'canciones')} · ${fmtDurationLong(totals.durationSeconds)}`
	);
</script>

<svelte:head>
	<title>Mis listas</title>
	<meta name="description" content="Mis listas en Musify." />
</svelte:head>

<Page>
	<PageHeader
		title="Mis listas"
		subtitle={items.length > 0 ? summary : 'Crea y organiza tus colecciones.'}
	>
		{#snippet actions()}
			{#if items.length > 0}
				<Button onclick={() => createPlaylistModal.show()}>Crear nueva lista</Button>
			{/if}
		{/snippet}
	</PageHeader>

	{#if items.length > 0}
		<MediaGrid min="210px" minMobile="180px" class="mt-7 sm:mt-8">
			{#each items as playlist, i (playlist.id)}
				<PlaylistCard
					id={playlist.id}
					name={playlist.name}
					trackCount={playlist.trackCount}
					trackIds={playlist.coverTrackIds}
					updatedAt={playlist.updatedAt}
					index={i}
				/>
			{/each}
		</MediaGrid>
	{:else}
		<EmptyState
			icon={ListMusic}
			title="Todavía no tienes listas"
			description="Crea la primera y añádele canciones de tu biblioteca."
		>
			{#snippet actions()}
				<Button onclick={() => createPlaylistModal.show()}>Crear nueva lista</Button>
			{/snippet}
		</EmptyState>
	{/if}
</Page>
