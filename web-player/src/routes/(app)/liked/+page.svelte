<script lang="ts">
	import Heart from '@lucide/svelte/icons/heart';
	import X from '@lucide/svelte/icons/x';
	import { player } from '$lib/player/player.svelte';
	import PlayAllButton from '$lib/components/ui/media/PlayAllButton.svelte';
	import { liked } from '$lib/player/liked.svelte';
	import { plural } from '$lib/utils/format';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import EmptyState from '$lib/components/ui/primitives/EmptyState.svelte';
	import TrackList from '$lib/components/ui/media/TrackList.svelte';

	const tracks = $derived(liked.list);
	const listTracks = $derived(tracks.map((track) => ({ ...track, date: track.likedAt })));

	function playFrom(index: number) {
		player.playOrToggle(tracks, index);
	}
</script>

<svelte:head>
	<title>Me gusta</title>
	<meta name="description" content="Las canciones que te gustan en Musify." />
</svelte:head>

<Page>
	<PageHeader
		eyebrow="Colección"
		title="Me gusta"
		meta={plural(tracks.length, 'canción', 'canciones')}
		align="center"
	>
		{#snippet cover()}
			<div
				class="grid size-cover-hero shrink-0 place-items-center rounded-art-lg bg-[image:var(--mf-liked-grad)] sm:size-cover-hero-sm"
			>
				<Heart class="size-icon-xl text-on-art/90" fill="currentColor" />
			</div>
		{/snippet}
		{#snippet actions()}
			<PlayAllButton items={tracks} />
		{/snippet}
	</PageHeader>

	{#if tracks.length > 0}
		<TrackList
			tracks={listTracks}
			columns={['added', 'plays']}
			onPlay={playFrom}
			rowAction={{
				onclick: (track) => liked.toggle(track),
				icon: X,
				label: 'Quitar de Me gusta'
			}}
		/>
	{:else}
		<EmptyState
			icon={Heart}
			title="Todavía no tienes canciones con me gusta"
			description="Toca el corazón en cualquier canción del reproductor para guardarla aquí."
		>
			{#snippet actions()}
				<Button href="/explore" variant="secondary">Explorar música</Button>
			{/snippet}
		</EmptyState>
	{/if}
</Page>
