<script lang="ts">
	import Plus from '@lucide/svelte/icons/plus';
	import Disc from '@lucide/svelte/icons/disc-3';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import MediaCard from '$lib/components/ui/media/MediaCard.svelte';
	import AlbumForm from '$lib/components/ui/forms/AlbumForm.svelte';
	import Modal from '$lib/components/ui/overlay/Modal.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import { albumMeta } from '$lib/utils/format';

	let { data, form } = $props();

	const items = $derived(data.albums.items);

	let creating = $state(false);
</script>

<svelte:head>
	<title>Álbumes</title>
	<meta name="description" content="Los álbumes que has publicado con tu música subida." />
</svelte:head>

<Page>
	<PageHeader title="Álbumes" description="Agrupa la música que subes en discos.">
		{#snippet actions()}
			{#if items.length > 0}
				<Button onclick={() => (creating = true)}>
					<Plus class="size-4" strokeWidth={2.5} />
					Nuevo álbum
				</Button>
			{/if}
		{/snippet}
	</PageHeader>

	{#if items.length > 0}
		<div class="grid-cards">
			{#each items as album, i (album.id)}
				<MediaCard
					href="/albums/{album.id}"
					title={album.title}
					subtitle={albumMeta(
						album.releaseYear === null ? undefined : Number(album.releaseYear),
						Number(album.trackCount)
					)}
					trackIds={album.coverTrackIds}
					src="/api/albums/{album.id}/cover?size=large"
					index={i}
				/>
			{/each}
		</div>
	{:else}
		<EmptyState
			icon={Disc}
			title="Todavía no tienes álbumes"
			description="Crea el primero y añádele las canciones que hayas subido."
		>
			{#snippet actions()}
				<Button onclick={() => (creating = true)}>Crear álbum</Button>
			{/snippet}
		</EmptyState>
	{/if}
</Page>

<Modal open={creating} onClose={() => (creating = false)} title="Nuevo álbum">
	<AlbumForm
		action="?/create"
		titlePlaceholder="Mi primer disco"
		requireCover
		formMessage={form?.message}
		submitLabel="Crear"
		submittingLabel="Creando…"
		onCancel={() => (creating = false)}
	/>
</Modal>
