<script lang="ts">
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import Shuffle from '@lucide/svelte/icons/shuffle';
	import {
		player,
		isQueueCurrent,
		playAllOrToggle,
		playShuffled,
		toQueueItem
	} from '$lib/player/player.svelte';
	import { fmtTime, plural } from '$lib/format';
	import Page from '$lib/components/ui/Page.svelte';
	import BackLink from '$lib/components/ui/BackLink.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import Artwork from '$lib/components/ui/Artwork.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import TrackList from '$lib/components/ui/TrackList.svelte';
	import ContextMenu from '$lib/components/ui/ContextMenu.svelte';
	import ListPlus from '@lucide/svelte/icons/list-plus';
	import { createTrackMenu } from '$lib/menus.svelte';
	import { mixItemTrack } from '$lib/mixes';

	let { data, form } = $props();

	const mix = $derived(data.mix);
	const items = $derived(mix.items);
	const tracks = $derived(items.map(mixItemTrack));
	const queue = $derived(tracks.map(toQueueItem));
	const listTracks = $derived(
		items.map((item) => ({
			id: item.trackId,
			title: item.title,
			artist: item.artist,
			duration: item.durationSeconds,
			listensCount: item.listensCount
		}))
	);

	const totalSeconds = $derived(
		items.reduce((total, item) => total + Number(item.durationSeconds), 0)
	);
	const isCurrentQueue = $derived(isQueueCurrent(queue));

	const trackMenu = createTrackMenu();

	function playFrom(index: number) {
		player.playOrToggle(queue, index);
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
		meta="{plural(items.length, 'canción', 'canciones')} · {fmtTime(totalSeconds)}"
	>
		{#snippet cover()}
			<Artwork
				trackIds={items.map((item) => item.trackId)}
				size="hero"
				alt={mix.title}
				class="shrink-0"
			/>
		{/snippet}
		{#snippet actions()}
			<Button size="sm" onclick={() => playAllOrToggle(queue)} disabled={queue.length === 0}>
				{#if isCurrentQueue && player.playing}
					<Pause class="h-4 w-4" strokeWidth={1.5} />
					Pausar
				{:else}
					<Play class="h-4 w-4" strokeWidth={1.5} />
					Reproducir
				{/if}
			</Button>
			<Button
				variant="secondary"
				size="sm"
				onclick={() => playShuffled(queue)}
				disabled={queue.length === 0}
			>
				<Shuffle class="h-4 w-4" />
				Aleatorio
			</Button>
		{/snippet}
	</PageHeader>

	{#if form?.message}
		<Alert tone="danger" class="mb-6">{form.message}</Alert>
	{/if}

	<TrackList
		tracks={listTracks}
		columns={['plays']}
		onPlay={playFrom}
		oncontextmenu={(e, _track, i) => trackMenu.open(e, tracks[i])}
	/>
</Page>

{#if trackMenu.state}
	<ContextMenu
		x={trackMenu.state.x}
		y={trackMenu.state.y}
		openLeft={trackMenu.state.openLeft}
		onClose={() => trackMenu.close()}
		items={[
			{ icon: ListPlus, label: 'Reproducir a continuación', onclick: () => trackMenu.playNext() }
		]}
		playlistAction={{
			action: '?/addTrack',
			fields: { trackId: String(trackMenu.state.track.id) },
			label: 'Añadir a una playlist'
		}}
		playlists={data.playlists}
	/>
{/if}
