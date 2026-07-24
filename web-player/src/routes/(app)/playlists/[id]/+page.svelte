<script lang="ts">
	import { player, queueIdForTrack, toQueueItems } from '$lib/player/player.svelte';
	import Page from '$lib/components/ui/Page.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import PlaylistForm from '$lib/components/ui/PlaylistForm.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import PlaylistHeader from './components/PlaylistHeader.svelte';
	import PlaylistTrackTable from './components/PlaylistTrackTable.svelte';
	import SuggestionsTable from './components/SuggestionsTable.svelte';
	import type { YouTubeSong } from '$lib/types';

	let { data, form } = $props();

	const playlist = $derived(data.playlist);
	const tracks = $derived(data.tracks);
	const library = $derived(data.library);

	let editing = $state(false);
	let confirmingDelete = $state(false);
	let youtubeSuggestions = $state<YouTubeSong[]>([]);

	$effect(() => {
		youtubeSuggestions = [];
		data.youtube.then((result) => {
			youtubeSuggestions = result.items;
		});
	});

	const isCurrentQueue = $derived(tracks.some((t) => queueIdForTrack(t) === player.current.id));

	function playAll() {
		if (tracks.length === 0) return;
		if (isCurrentQueue) player.toggle();
		else player.playQueue(toQueueItems(tracks), 0);
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
		playing={isCurrentQueue && player.playing}
		onPlayAll={playAll}
		onEdit={() => (editing = true)}
		onDelete={() => (confirmingDelete = true)}
	/>

	{#if tracks.length > 0}
		<PlaylistTrackTable {tracks} />
	{/if}

	{#if library.length > 0 || youtubeSuggestions.length > 0}
		<SuggestionsTable {library} youtube={youtubeSuggestions} playlists={data.playlists} />
	{/if}
</Page>

<Modal open={editing} onClose={() => (editing = false)} title="Editar playlist">
	<PlaylistForm
		action="?/rename"
		initialName={playlist.name}
		initialDescription={playlist.description ?? ''}
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
	<h2 class="text-lg font-semibold tracking-tight text-fg">
		¿Eliminar «{playlist.name}»?
	</h2>
	<p class="mt-2 text-base text-fg-3">
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
