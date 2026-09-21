<script lang="ts">
	import Upload from '@lucide/svelte/icons/upload';
	import X from '@lucide/svelte/icons/x';
	import Music from '@lucide/svelte/icons/music';
	import { player, toQueueItems } from '$lib/player/player.svelte';
	import { plural } from '$lib/format';
	import Page from '$lib/components/ui/Page.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import TrackList from '$lib/components/ui/TrackList.svelte';

	let { data } = $props();

	const items = $derived(data.tracks.items);
	const total = $derived(Number(data.tracks.totalItemCount));
	const listTracks = $derived(items.map((track) => ({ ...track, uploadedAt: track.createdAt })));

	function togglePlay(index: number) {
		player.playOrToggle(toQueueItems(items), index);
	}
</script>

<svelte:head>
	<title>Tus canciones subidas</title>
	<meta name="description" content="Todas tus canciones subidas a Musify." />
</svelte:head>

<Page>
	<PageHeader
		title="Tus canciones subidas"
		description="{plural(total, 'canción', 'canciones')} subida{total === 1 ? '' : 's'}."
	>
		{#snippet actions()}
			{#if items.length > 0}
				<Button href="/upload">
					<Upload class="size-4" />
					Subir canción
				</Button>
			{/if}
		{/snippet}
	</PageHeader>

	{#if items.length > 0}
		<TrackList
			tracks={listTracks}
			columns={['uploaded', 'plays']}
			onPlay={togglePlay}
			rowAction={{ action: '?/deleteTrack', icon: X, label: 'Borrar canción' }}
		/>
	{:else}
		<EmptyState
			icon={Music}
			title="Todavía no tienes canciones"
			description="Sube la primera y aparecerá aquí lista para reproducir."
		>
			{#snippet actions()}
				<Button href="/upload">
					<Upload class="size-4" />
					Sube la primera
				</Button>
			{/snippet}
		</EmptyState>
	{/if}
</Page>
