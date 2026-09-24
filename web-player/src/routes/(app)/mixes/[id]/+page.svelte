<script lang="ts">
	import Shuffle from '@lucide/svelte/icons/shuffle';
	import {
		player,
		isQueueCurrent,
		playAllOrToggle,
		playShuffled,
		toQueueItem
	} from '$lib/player/player.svelte';
	import { fmtTime, plural } from '$lib/utils/format';
	import Page from '$lib/components/ui/layout/Page.svelte';
	import BackLink from '$lib/components/ui/primitives/BackLink.svelte';
	import Button from '$lib/components/ui/primitives/Button.svelte';
	import Alert from '$lib/components/ui/primitives/Alert.svelte';
	import Artwork from '$lib/components/ui/media/Artwork.svelte';
	import PageHeader from '$lib/components/ui/layout/PageHeader.svelte';
	import TrackList from '$lib/components/ui/media/TrackList.svelte';
	import ContextMenu from '$lib/components/ui/overlay/ContextMenu.svelte';
	import ListPlus from '@lucide/svelte/icons/list-plus';
	import { createTrackMenu } from '$lib/state/menus.svelte';
	import { mixItemTrack, mixText } from '$lib/data/mixes';

	let { data, form } = $props();

	const mix = $derived(data.mix);
	const text = $derived(mixText(mix));
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
	<title>{text.title}</title>
	<meta name="description" content="Mezcla {text.title}, hecha para ti." />
</svelte:head>

<Page>
	<BackLink href="/" label="Volver al inicio" />

	<PageHeader
		eyebrow="Mezcla"
		title={text.title}
		description={text.subtitle}
		meta="{plural(items.length, 'canción', 'canciones')} · {fmtTime(totalSeconds)}"
	>
		{#snippet cover()}
			<Artwork
				trackIds={items.map((item) => item.trackId)}
				size="hero"
				alt={text.title}
				class="shrink-0"
			/>
		{/snippet}
		{#snippet actions()}
			<Button size="sm" onclick={() => playAllOrToggle(queue)} disabled={queue.length === 0}>
				{#if isCurrentQueue && player.playing}
					Pausar
				{:else}
					Reproducir
				{/if}
			</Button>
			<Button
				variant="secondary"
				size="sm"
				onclick={() => playShuffled(queue)}
				disabled={queue.length === 0}
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
