<script lang="ts">
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import Shuffle from '@lucide/svelte/icons/shuffle';
	import { player, isQueueCurrent, playAllOrToggle, playShuffled } from '$lib/player/player.svelte';
	import { fmtTime, plural } from '$lib/format';
	import Page from '$lib/components/ui/Page.svelte';
	import BackLink from '$lib/components/ui/BackLink.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import Artwork from '$lib/components/ui/Artwork.svelte';
	import CollectionHeader from '$lib/components/ui/CollectionHeader.svelte';
	import TrackTable from '$lib/components/ui/TrackTable.svelte';
	import TrackRow from '$lib/components/ui/TrackRow.svelte';
	import TrackMeta from '$lib/components/ui/TrackMeta.svelte';
	import TrackIndexCell from '$lib/components/ui/TrackIndexCell.svelte';
	import MediaIdentity from '$lib/components/ui/MediaIdentity.svelte';
	import { pressable } from '$lib/actions/pressable';
	import ContextMenu from '$lib/components/ui/ContextMenu.svelte';
	import ListPlus from '@lucide/svelte/icons/list-plus';
	import { createTrackMenu } from '$lib/menus.svelte';
	import { mixItemKey, mixItemTarget } from '$lib/mixes';
	import {
		isTargetCurrent,
		queueItemForTarget,
		targetArtist,
		targetId,
		targetListensCount,
		targetTitle
	} from '$lib/tracks';

	let { data, form } = $props();

	const mix = $derived(data.mix);
	const items = $derived(mix.items);
	const targets = $derived(items.map(mixItemTarget));
	const queue = $derived(targets.map(queueItemForTarget));

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

	<CollectionHeader
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
	</CollectionHeader>

	{#if form?.message}
		<Alert tone="danger" class="mt-4">{form.message}</Alert>
	{/if}

	<TrackTable index meta={[{ label: 'Escuchas', width: '100px' }]} class="mt-6 sm:mt-8">
		{#each items as item, i (mixItemKey(item))}
			{@const active = isTargetCurrent(targets[i])}
			<TrackRow {active} oncontextmenu={(e) => trackMenu.open(e, targets[i])}>
				<TrackIndexCell index={i} {active} playing={player.playing} onToggle={() => playFrom(i)} />
				<!-- svelte-ignore a11y_click_events_have_key_events -->
				<div
					role="button"
					tabindex="0"
					use:pressable={() => playFrom(i)}
					onclick={(event) => event.stopPropagation()}
					class="flex min-w-0 flex-1 text-left"
				>
					<MediaIdentity
						trackId={targetId(targets[i])}
						title={targetTitle(targets[i])}
						artist={targetArtist(targets[i])}
						{active}
					/>
				</div>
				<TrackMeta class="hidden sm:block">{Number(targetListensCount(targets[i]) ?? 0)}</TrackMeta>
				<TrackMeta>{fmtTime(Number(item.durationSeconds))}</TrackMeta>
			</TrackRow>
		{/each}
	</TrackTable>
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
