<script lang="ts">
	import Shuffle from '@lucide/svelte/icons/shuffle';
	import { player } from '$lib/player/player.svelte';
	import { playShuffled } from '$lib/player/actions';
	import { fmtTime, plural } from '$lib/utils/format';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import BackLink from '$lib/components/ui/primitives/BackLink.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import Artwork from '$lib/components/ui/media/Artwork.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import TrackList from '$lib/components/ui/media/TrackList.svelte';
	import TrackContextMenu from '$lib/components/ui/overlay/TrackContextMenu.svelte';
	import { createMenu } from '$lib/state/menu.svelte';
	import type { Track } from '$lib/types';
	import PlayAllButton from '$lib/components/ui/media/PlayAllButton.svelte';

	let { data, form } = $props();

	const mix = $derived(data.mix);
	const tracks = $derived(mix.tracks);

	const totalSeconds = $derived(tracks.reduce((total, track) => total + track.duration, 0));

	const trackMenu = createMenu<Track>();

	function playFrom(index: number) {
		player.playOrToggle(tracks, index);
	}
</script>

<svelte:head>
	<title>{mix.title}</title>
	<meta name="description" content="Mezcla {mix.title}, hecha para ti." />
</svelte:head>

<Page>
	<BackLink href="/" label="Volver al inicio" />

	<PageHeader
		eyebrow="Mezcla"
		title={mix.title}
		description={mix.subtitle ?? undefined}
		meta="{plural(tracks.length, 'canción', 'canciones')} · {fmtTime(totalSeconds)}"
	>
		{#snippet cover()}
			<Artwork
				trackIds={tracks.map((track) => track.id)}
				size="hero"
				alt={mix.title}
				class="shrink-0"
			/>
		{/snippet}
		{#snippet actions()}
			<PlayAllButton items={tracks} />
			<Button
				variant="secondary"
				size="sm"
				onclick={() => playShuffled(tracks)}
				disabled={tracks.length === 0}
			>
				<Shuffle class="size-4" />
				Aleatorio
			</Button>
		{/snippet}
	</PageHeader>

	{#if form?.message}
		<Alert tone="danger" class="mb-6">{form.message}</Alert>
	{/if}

	<TrackList
		{tracks}
		columns={['plays']}
		onPlay={playFrom}
		oncontextmenu={(e, track) => trackMenu.open(e, track)}
	/>
</Page>

<TrackContextMenu menu={trackMenu} playlists={data.userPlaylists} />
