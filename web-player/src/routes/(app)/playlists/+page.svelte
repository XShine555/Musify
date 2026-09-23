<script lang="ts">
	import ListMusic from '@lucide/svelte/icons/list-music';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import MediaCard from '$lib/components/ui/media/MediaCard.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import { createPlaylistModal } from '$lib/state/playlists.svelte';
	import { fmtDurationLong, plural, playlistMeta } from '$lib/utils/format';

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
		description={items.length > 0 ? summary : 'Crea y organiza tus colecciones.'}
	>
		{#snippet actions()}
			{#if items.length > 0}
				<Button onclick={() => createPlaylistModal.show()}>Crear nueva lista</Button>
			{/if}
		{/snippet}
	</PageHeader>

	{#if items.length > 0}
		<div class="grid-cards-lg">
			{#each items as playlist, i (playlist.id)}
				<MediaCard
					href="/playlists/{playlist.id}"
					title={playlist.name}
					subtitle={playlistMeta(Number(playlist.trackCount))}
					trackIds={playlist.coverTrackIds}
					src="/api/playlists/{playlist.id}/cover?size=large&v={encodeURIComponent(
						playlist.updatedAt
					)}"
					index={i}
				/>
			{/each}
		</div>
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
