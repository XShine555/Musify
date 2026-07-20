<script lang="ts">
	import Plus from '@lucide/svelte/icons/plus';
	import ListMusic from '@lucide/svelte/icons/list-music';
	import Page from '$lib/components/ui/Page.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import MediaGrid from '$lib/components/ui/MediaGrid.svelte';
	import PlaylistCard from '$lib/components/ui/PlaylistCard.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import PlaylistForm from '$lib/components/ui/PlaylistForm.svelte';
	import Button from '$lib/components/ui/Button.svelte';

	const playlistNames = [
		'Mi Playlist',
		'Tus favoritos',
		'Lo mejor de hoy',
		'Para relajarse',
		'Buena vibra',
		'Modo estudio',
		'Road Trip',
		'Domingos',
		'Late Night',
		'Descubriendo música',
		'Todo un poco',
		'En bucle',
		'Mi colección',
		'Canciones para repetir',
		'Hits del momento'
	];
	const namePlaceholder = playlistNames[Math.floor(Math.random() * playlistNames.length)];

	let { data, form } = $props();

	const items = $derived(data.playlists.items);

	let creating = $state(false);

	function openCreate() {
		creating = true;
	}
</script>

<svelte:head>
	<title>Mis listas · Musify</title>
	<meta name="description" content="Mis listas en Musify." />
</svelte:head>

<Page>
	<PageHeader title="Mis listas" subtitle="Crea y organiza tus colecciones.">
		{#snippet actions()}
			{#if items.length > 0}
				<Button onclick={openCreate}>
					<Plus class="h-4 w-4" strokeWidth={2.5} />
					Crear lista
				</Button>
			{/if}
		{/snippet}
	</PageHeader>

	{#if items.length > 0}
		<MediaGrid class="mt-9">
			{#each items as playlist, i (playlist.id)}
				<PlaylistCard
					id={playlist.id}
					name={playlist.name}
					description={playlist.description}
					trackIds={playlist.coverTrackIds}
					updatedAt={playlist.updatedAt}
					index={i}
				/>
			{/each}
		</MediaGrid>
	{:else}
		<EmptyState
			icon={ListMusic}
			title="Todavía no tienes listas"
			description="Crea la primera y añádele canciones de tu biblioteca."
		>
			{#snippet actions()}
				<Button onclick={openCreate}>
					<Plus class="h-4 w-4" strokeWidth={2} />
					Crear lista
				</Button>
			{/snippet}
		</EmptyState>
	{/if}
</Page>

<Modal open={creating} onClose={() => (creating = false)} title="Nueva lista">
	<PlaylistForm
		action="?/create"
		{namePlaceholder}
		formMessage={form?.message}
		submitLabel="Crear"
		submittingLabel="Creando…"
		onCancel={() => (creating = false)}
	/>
</Modal>
