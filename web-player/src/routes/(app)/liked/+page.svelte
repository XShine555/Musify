<script lang="ts">
	import Heart from '@lucide/svelte/icons/heart';
	import X from '@lucide/svelte/icons/x';
	import { player, isQueueCurrent, playAllOrToggle } from '$lib/player/player.svelte';
	import { liked } from '$lib/player/liked.svelte';
	import { plural } from '$lib/format';
	import Page from '$lib/components/ui/Page.svelte';
	import CollectionHeader from '$lib/components/ui/CollectionHeader.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import TrackList from '$lib/components/ui/TrackList.svelte';

	const tracks = $derived(liked.list);
	const listTracks = $derived(tracks.map((track) => ({ ...track, addedAt: track.likedAt })));
	const isCurrentQueue = $derived(isQueueCurrent(tracks));

	function playFrom(index: number) {
		player.playOrToggle(tracks, index);
	}

	function playAll() {
		playAllOrToggle(tracks);
	}
</script>

<svelte:head>
	<title>Me gusta</title>
	<meta name="description" content="Las canciones que te gustan en Musify." />
</svelte:head>

<Page>
	<CollectionHeader
		eyebrow="Colección"
		title="Me gusta"
		meta={plural(tracks.length, 'canción', 'canciones')}
		align="center"
	>
		{#snippet cover()}
			<div
				class="grid h-36 w-36 shrink-0 place-items-center rounded-art-lg bg-[image:var(--mf-liked-grad)] sm:h-41 sm:w-41"
			>
				<Heart class="h-10 w-10 text-on-art/90" fill="currentColor" strokeWidth={0} />
			</div>
		{/snippet}
		{#snippet actions()}
			<Button size="sm" onclick={playAll} disabled={tracks.length === 0}>
				{isCurrentQueue && player.playing ? 'Pausar' : 'Reproducir'}
			</Button>
		{/snippet}
	</CollectionHeader>

	{#if tracks.length > 0}
		<TrackList
			tracks={listTracks}
			columns={['added', 'plays']}
			onPlay={playFrom}
			rowAction={{
				onclick: (track) => liked.toggle({ ...track, artist: track.artist ?? undefined }),
				icon: X,
				label: 'Quitar de Me gusta'
			}}
			class="mt-6 sm:mt-8"
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
