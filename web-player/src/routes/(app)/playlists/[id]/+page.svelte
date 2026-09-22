<script lang="ts">
	import ListMusic from '@lucide/svelte/icons/list-music';
	import X from '@lucide/svelte/icons/x';
	import { player, toQueueItems, isQueueCurrent, playAllOrToggle } from '$lib/player/player.svelte';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import Modal from '$lib/components/ui/overlay/Modal.svelte';
	import ConfirmDialog from '$lib/components/ui/overlay/ConfirmDialog.svelte';
	import Artwork from '$lib/components/ui/media/Artwork.svelte';
	import PlaylistForm from '$lib/components/ui/forms/PlaylistForm.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import TrackList from '$lib/components/ui/media/TrackList.svelte';
	import { playlistMeta } from '$lib/utils/format';

	let { data, form } = $props();

	const playlist = $derived(data.playlist);
	const tracks = $derived(data.tracks);
	const listTracks = $derived(tracks.map((track) => ({ ...track, addedAt: track.createdAt })));

	let editing = $state(false);
	let confirmingDelete = $state(false);

	const isCurrentQueue = $derived(isQueueCurrent(tracks));

	function playAll() {
		playAllOrToggle(toQueueItems(tracks));
	}

	function playFrom(index: number) {
		player.playOrToggle(toQueueItems(tracks), index);
	}
</script>

<svelte:head>
	<title>{playlist.name}</title>
	<meta name="description" content="Playlist {playlist.name} en Musify." />
</svelte:head>

<Page>
	<PageHeader
		eyebrow="Lista"
		title={playlist.name}
		description={playlist.description ?? undefined}
		meta="{playlistMeta(tracks.length)} · {playlist.visibility === 'Public'
			? 'Pública'
			: 'Privada'}"
		align="center"
	>
		{#snippet cover()}
			<Artwork
				src="/api/playlists/{playlist.id}/cover?size=large&v={encodeURIComponent(
					playlist.updatedAt
				)}"
				trackIds={tracks.map((t) => t.id)}
				size="hero"
				alt={playlist.name}
				class="shrink-0"
			/>
		{/snippet}
		{#snippet actions()}
			<Button size="sm" onclick={playAll} disabled={tracks.length === 0}>
				{#if isCurrentQueue && player.playing}
					Pausar
				{:else}
					Reproducir
				{/if}
			</Button>
			<Button size="sm" variant="secondary" onclick={() => (editing = true)}>
				Editar
			</Button>
			<Button size="sm" variant="secondary" onclick={() => (confirmingDelete = true)}>
				Eliminar
			</Button>
		{/snippet}
	</PageHeader>

	{#if tracks.length > 0}
		<TrackList
			tracks={listTracks}
			columns={['added', 'plays']}
			onPlay={playFrom}
			rowAction={{ action: '?/removeTrack', icon: X, label: 'Quitar de la playlist' }}
		/>
	{:else}
		<EmptyState
			icon={ListMusic}
			title="Esta playlist todavía está vacía"
			description="Añade canciones desde tu biblioteca o explora música nueva para empezar."
		>
			{#snippet actions()}
				<Button href="/explore" variant="secondary">Explorar música</Button>
			{/snippet}
		</EmptyState>
	{/if}
</Page>

<Modal open={editing} onClose={() => (editing = false)} title="Editar playlist">
	<PlaylistForm
		action="?/rename"
		initialName={playlist.name}
		initialDescription={playlist.description ?? ''}
		initialVisibility={playlist.visibility === 'Public' ? 'public' : 'private'}
		coverFallbackUrl="/api/playlists/{playlist.id}/cover?size=medium&v={encodeURIComponent(
			playlist.updatedAt
		)}"
		formMessage={form?.message}
		submitLabel="Guardar"
		submittingLabel="Guardando…"
		onCancel={() => (editing = false)}
		onSuccess={() => (editing = false)}
	/>
</Modal>

<ConfirmDialog
	open={confirmingDelete}
	onClose={() => (confirmingDelete = false)}
	title="¿Eliminar «{playlist.name}»?"
	description="Esta acción no se puede deshacer, la playlist se eliminará de tu biblioteca y de todos los dispositivos donde la tengas guardada."
	confirmLabel="Eliminar"
	action="?/delete"
/>
