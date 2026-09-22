<script lang="ts">
	import { player, toQueueItems, isQueueCurrent, playAllOrToggle } from '$lib/player/player.svelte';
	import X from '@lucide/svelte/icons/x';
	import Plus from '@lucide/svelte/icons/plus';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import Modal from '$lib/components/ui/overlay/Modal.svelte';
	import ConfirmDialog from '$lib/components/ui/overlay/ConfirmDialog.svelte';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import Artwork from '$lib/components/ui/media/Artwork.svelte';
	import AlbumForm from '$lib/components/ui/forms/AlbumForm.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import BackLink from '$lib/components/ui/primitives/BackLink.svelte';
	import SectionHeading from '$lib/components/ui/layout/SectionHeading.svelte';
	import TrackList from '$lib/components/ui/media/TrackList.svelte';
	import { albumMeta } from '$lib/utils/format';

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
					Pausar
				{:else}
					Reproducir
				{/if}
			</Button>
			{#if isOwner}
				<Button size="sm" variant="secondary" onclick={() => (editing = true)}>
					Editar
				</Button>
				<Button size="sm" variant="secondary" onclick={() => (confirmingDelete = true)}>
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
