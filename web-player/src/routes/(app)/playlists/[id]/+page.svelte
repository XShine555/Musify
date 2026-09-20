<script lang="ts">
	import ListMusic from '@lucide/svelte/icons/list-music';
	import { player, toQueueItems, isQueueCurrent, playAllOrToggle } from '$lib/player/player.svelte';
	import Page from '$lib/components/ui/Page.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import PlaylistForm from '$lib/components/ui/PlaylistForm.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import PlaylistHeader from './components/PlaylistHeader.svelte';
	import PlaylistTrackTable from './components/PlaylistTrackTable.svelte';

	let { data, form } = $props();

	const playlist = $derived(data.playlist);
	const tracks = $derived(data.tracks);

	let editing = $state(false);
	let confirmingDelete = $state(false);

	const isCurrentQueue = $derived(isQueueCurrent(tracks));

	function playAll() {
		playAllOrToggle(toQueueItems(tracks));
	}
</script>

<svelte:head>
	<title>{playlist.name}</title>
	<meta name="description" content="Playlist {playlist.name} en Musify." />
</svelte:head>

<Page>
	<PlaylistHeader
		id={playlist.id}
		name={playlist.name}
		description={playlist.description}
		trackIds={tracks.map((t) => t.id)}
		updatedAt={playlist.updatedAt}
		visibility={playlist.visibility}
		playing={isCurrentQueue && player.playing}
		onPlayAll={playAll}
		onEdit={() => (editing = true)}
		onDelete={() => (confirmingDelete = true)}
	/>

	{#if tracks.length > 0}
		<PlaylistTrackTable {tracks} />
	{:else}
		<EmptyState
			icon={ListMusic}
			title="Esta playlist todavía está vacía"
			description="Añade canciones desde tu biblioteca o explora música nueva para empezar."
			class="mt-6 sm:mt-8"
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

<Modal open={confirmingDelete} onClose={() => (confirmingDelete = false)} maxWidth="max-w-sm">
	<h2 class="font-display text-xl font-semibold tracking-[-0.02em] text-fg">
		¿Eliminar «{playlist.name}»?
	</h2>
	<p class="mt-2.5 text-sm leading-[1.65] text-fg-2">
		Esta acción no se puede deshacer, la playlist se eliminará de tu biblioteca y de todos los
		dispositivos donde la tengas guardada.
	</p>
	<div class="mt-6 flex items-center justify-end gap-3">
		<Button variant="secondary" onclick={() => (confirmingDelete = false)}>Cancelar</Button>
		<form method="POST" action="?/delete">
			<Button type="submit" variant="danger">Eliminar</Button>
		</form>
	</div>
</Modal>
