<script lang="ts" module>
	const SECTIONS = [
		{ value: 'tracks', label: 'Canciones' },
		{ value: 'albums', label: 'Álbumes' },
		{ value: 'uploads', label: 'Subidas' }
	] as const;

	type Section = (typeof SECTIONS)[number]['value'];
</script>

<script lang="ts">
	import { page } from '$app/state';
	import { plural } from '$lib/utils/format';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import LibraryTracksSection from '$lib/components/music/library/LibraryTracksSection.svelte';
	import LibraryAlbumsSection from '$lib/components/music/library/LibraryAlbumsSection.svelte';
	import LibraryUploadsSection from '$lib/components/music/library/LibraryUploadsSection.svelte';

	let { data, form } = $props();

	const items = $derived(data.tracks.items);
	const total = $derived(data.tracks.totalItemCount);

	let section = $state<Section>(
		SECTIONS.find((option) => option.value === page.url.searchParams.get('section'))?.value ??
			'tracks'
	);
	let resetKey = $state(0);

	function selectSection(value: Section) {
		if (value === section) resetKey += 1;
		section = value;
	}
</script>

<svelte:head>
	<title>Gestionar música</title>
	<meta name="description" content="Todas tus canciones subidas a Musify." />
</svelte:head>

<Page>
	<PageHeader
		title="Gestionar música"
		description="{plural(total, 'canción', 'canciones')} subida{total === 1
			? ''
			: 's'}. Puedes borrarlas o reproducirlas desde aquí."
	/>

	<div class="flex gap-2">
		{#each SECTIONS as option (option.value)}
			<Button
				variant={section === option.value ? 'primary' : 'secondary'}
				onclick={() => selectSection(option.value)}
				size="sm"
			>
				{option.label}
			</Button>
		{/each}
	</div>

	{#if section === 'tracks'}
		<LibraryTracksSection
			tracks={items}
			errorMessage={form?.action === 'deleteTracks' ? form.message : undefined}
		/>
	{:else if section === 'albums'}
		{#key resetKey}
			<LibraryAlbumsSection
				albums={data.albums.items}
				tracks={items}
				albumTrackIds={data.albumTrackIds}
				formMessage={form?.action === 'saveAlbum' || form?.action === 'deleteAlbum'
					? form.message
					: undefined}
			/>
		{/key}
	{:else}
		<LibraryUploadsSection genres={data.genres} />
	{/if}
</Page>
