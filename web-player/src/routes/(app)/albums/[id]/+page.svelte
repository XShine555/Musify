<script lang="ts">
	import { player, toQueueItems, isQueueCurrent, playAllOrToggle } from '$lib/player/player.svelte';
	import X from '@lucide/svelte/icons/x';
	import Plus from '@lucide/svelte/icons/plus';
	import Page from '$lib/components/ui/Page.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import AlbumForm from '$lib/components/ui/AlbumForm.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';
	import TrackList from '$lib/components/ui/TrackList.svelte';
	import AlbumHeader from './components/AlbumHeader.svelte';

	let { data, form } = $props();

	const album = $derived(data.album);
	const tracks = $derived(data.tracks);
	const library = $derived(data.library);
	const isOwner = $derived(data.isOwner);

	let editing = $state(false);
	let confirmingDelete = $state(false);

	const isCurrentQueue = $derived(isQueueCurrent(tracks));

	function playAll() {
		playAllOrToggle(toQueueItems(tracks));
	}

	function playTrackFrom(index: number) {
		player.playOrToggle(toQueueItems(tracks), index);
	}

	function playLibraryFrom(index: number) {
		player.playOrToggle(toQueueItems(library), index);
	}
</script>

<svelte:head>
	<title>{album.title}</title>
	<meta name="description" content="Álbum {album.title}." />
</svelte:head>

<Page>
	<AlbumHeader
		title={album.title}
		description={album.description ?? undefined}
		releaseYear={album.releaseYear === null ? undefined : Number(album.releaseYear)}
		trackIds={tracks.map((track) => track.id)}
		coverUrl="/api/albums/{album.id}/cover?size=large"
		playing={isCurrentQueue && player.playing}
		{isOwner}
		onPlayAll={playAll}
		onEdit={() => (editing = true)}
		onDelete={() => (confirmingDelete = true)}
	/>

	{#if form?.message}
		<Alert tone="danger" class="mt-6">{form.message}</Alert>
	{/if}

	{#if tracks.length > 0}
		<TrackList
			{tracks}
			onPlay={playTrackFrom}
			rowAction={isOwner
				? { action: '?/removeTrack', icon: X, label: 'Quitar del álbum' }
				: undefined}
			class="mt-6 sm:mt-8"
		/>
	{/if}

	{#if isOwner && library.length > 0}
		<div class="mt-10 sm:mt-12">
			<SectionHeading title="Tus canciones subidas" />
			<TrackList
				tracks={library}
				index={false}
				onPlay={playLibraryFrom}
				rowAction={{ action: '?/addTrack', icon: Plus, label: 'Añadir al álbum' }}
			/>
		</div>
	{/if}
</Page>

<Modal open={editing} onClose={() => (editing = false)} title="Editar álbum">
	<AlbumForm
		action="?/edit"
		initialTitle={album.title}
		initialDescription={album.description ?? ''}
		initialReleaseYear={album.releaseYear === null ? undefined : Number(album.releaseYear)}
		coverFallbackUrl="/api/albums/{album.id}/cover?size=medium"
		formMessage={form?.message}
		submitLabel="Guardar"
		submittingLabel="Guardando…"
		onCancel={() => (editing = false)}
		onSuccess={() => (editing = false)}
	/>
</Modal>

<Modal open={confirmingDelete} onClose={() => (confirmingDelete = false)} maxWidth="max-w-sm">
	<h2 class="text-lg font-semibold tracking-tight text-fg">
		¿Eliminar «{album.title}»?
	</h2>
	<p class="mt-2.5 text-sm leading-[1.65] text-fg-2">
		Esta acción no se puede deshacer. Las canciones que contiene seguirán en tu biblioteca.
	</p>
	<div class="mt-6 flex items-center justify-end gap-3">
		<Button variant="secondary" onclick={() => (confirmingDelete = false)}>Cancelar</Button>
		<form method="POST" action="?/delete">
			<Button type="submit" variant="danger">Eliminar</Button>
		</form>
	</div>
</Modal>
