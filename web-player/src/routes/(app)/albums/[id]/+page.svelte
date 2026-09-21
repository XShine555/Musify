<script lang="ts">
	import { player, toQueueItems, isQueueCurrent, playAllOrToggle } from '$lib/player/player.svelte';
	import X from '@lucide/svelte/icons/x';
	import Plus from '@lucide/svelte/icons/plus';
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import SquarePencil from '@lucide/svelte/icons/square-pen';
	import Trash from '@lucide/svelte/icons/trash';
	import Page from '$lib/components/ui/Page.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import ConfirmDialog from '$lib/components/ui/ConfirmDialog.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import Artwork from '$lib/components/ui/Artwork.svelte';
	import AlbumForm from '$lib/components/ui/AlbumForm.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import BackLink from '$lib/components/ui/BackLink.svelte';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';
	import TrackList from '$lib/components/ui/TrackList.svelte';
	import { albumMeta } from '$lib/format';

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
	<BackLink
		href={isOwner ? '/albums' : '/'}
		label={isOwner ? 'Volver a tus álbumes' : 'Volver al inicio'}
	/>

	<PageHeader
		eyebrow="Álbum"
		title={album.title}
		description={album.description ?? undefined}
		meta={albumMeta(
			album.releaseYear === null ? undefined : Number(album.releaseYear),
			tracks.length
		)}
	>
		{#snippet cover()}
			<Artwork
				src="/api/albums/{album.id}/cover?size=large"
				trackIds={tracks.map((track) => track.id)}
				size="hero"
				alt={album.title}
				class="shrink-0"
			/>
		{/snippet}
		{#snippet actions()}
			<Button size="sm" onclick={playAll} disabled={tracks.length === 0}>
				{#if isCurrentQueue && player.playing}
					<Pause class="size-4" strokeWidth={1.5} />
					Pausar
				{:else}
					<Play class="size-4" strokeWidth={1.5} />
					Reproducir
				{/if}
			</Button>
			{#if isOwner}
				<Button size="sm" variant="secondary" onclick={() => (editing = true)}>
					<SquarePencil class="size-4" strokeWidth={1.5} />
					Editar
				</Button>
				<Button size="sm" variant="secondary" onclick={() => (confirmingDelete = true)}>
					<Trash class="size-4" strokeWidth={1.5} />
					Eliminar
				</Button>
			{/if}
		{/snippet}
	</PageHeader>

	{#if form?.message}
		<Alert tone="danger" class="mb-6">{form.message}</Alert>
	{/if}

	{#if tracks.length > 0}
		<TrackList
			{tracks}
			onPlay={playTrackFrom}
			rowAction={isOwner
				? { action: '?/removeTrack', icon: X, label: 'Quitar del álbum' }
				: undefined}
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

<ConfirmDialog
	open={confirmingDelete}
	onClose={() => (confirmingDelete = false)}
	title="¿Eliminar «{album.title}»?"
	description="Esta acción no se puede deshacer. Las canciones que contiene seguirán en tu biblioteca."
	confirmLabel="Eliminar"
	action="?/delete"
/>
