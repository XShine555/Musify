<script lang="ts">
	import Plus from '@lucide/svelte/icons/plus';
	import Disc from '@lucide/svelte/icons/disc-3';
	import Page from '$lib/components/ui/Page.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import MediaGrid from '$lib/components/ui/MediaGrid.svelte';
	import AlbumCard from '$lib/components/ui/AlbumCard.svelte';
	import AlbumForm from '$lib/components/ui/AlbumForm.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import Button from '$lib/components/ui/Button.svelte';

	let { data, form } = $props();

	const items = $derived(data.albums.items);

	let creating = $state(false);
</script>

<svelte:head>
	<title>Álbumes</title>
	<meta name="description" content="Los álbumes que has publicado con tu música subida." />
</svelte:head>

<Page>
	<PageHeader title="Álbumes" subtitle="Agrupa la música que subes en discos.">
		{#snippet actions()}
			{#if items.length > 0}
				<Button onclick={() => (creating = true)}>
					<Plus class="h-4 w-4" strokeWidth={2.5} />
					Nuevo álbum
				</Button>
			{/if}
		{/snippet}
	</PageHeader>

	{#if items.length > 0}
		<MediaGrid class="mt-9">
			{#each items as album, i (album.id)}
				<AlbumCard
					id={album.id}
					title={album.title}
					releaseYear={album.releaseYear === null ? undefined : Number(album.releaseYear)}
					trackCount={Number(album.trackCount)}
					trackIds={album.coverTrackIds}
					coverSrc="/api/albums/{album.id}/cover?size=large"
					index={i}
				/>
			{/each}
		</MediaGrid>
	{:else}
		<EmptyState
			icon={Disc}
			title="Todavía no tienes álbumes"
			description="Crea el primero y añádele las canciones que hayas subido."
		>
			{#snippet actions()}
				<Button onclick={() => (creating = true)}>
					<Plus class="h-4 w-4" strokeWidth={2} />
					Crear álbum
				</Button>
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
