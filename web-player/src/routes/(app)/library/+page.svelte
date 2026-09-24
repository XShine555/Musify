<script lang="ts">
	import Upload from '@lucide/svelte/icons/upload';
	import X from '@lucide/svelte/icons/x';
	import Music from '@lucide/svelte/icons/music';
	import { player, toQueueItems } from '$lib/player/player.svelte';
	import { plural } from '$lib/utils/format';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import ConfirmDialog from '$lib/components/ui/overlay/ConfirmDialog.svelte';
	import TrackList from '$lib/components/ui/media/TrackList.svelte';

	let { data } = $props();

	const items = $derived(data.tracks.items);
	const total = $derived(Number(data.tracks.totalItemCount));
	const listTracks = $derived(items.map((track) => ({ ...track, uploadedAt: track.createdAt })));

	let deleting = $state<(typeof items)[number] | null>(null);

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
			rowAction={{
				onclick: (track) => (deleting = items.find((item) => item.id === track.id) ?? null),
				icon: X,
				label: 'Borrar canción'
			}}
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

<ConfirmDialog
	open={deleting !== null}
	onClose={() => (deleting = null)}
	title="¿Borrar «{deleting?.title ?? ''}»?"
	description="Esta acción no se puede deshacer, la canción se eliminará de tu biblioteca, de tus álbumes y de las playlists donde esté."
	confirmLabel="Borrar"
	action="?/deleteTrack"
	fields={{ trackId: deleting?.id ?? '' }}
/>
