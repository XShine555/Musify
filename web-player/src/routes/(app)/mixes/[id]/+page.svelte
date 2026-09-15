<script lang="ts">
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import Shuffle from '@lucide/svelte/icons/shuffle';
	import { player } from '$lib/player/player.svelte';
	import { fmtTime } from '$lib/format';
	import { shuffle } from '$lib/collections';
	import Page from '$lib/components/ui/Page.svelte';
	import BackLink from '$lib/components/ui/BackLink.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import MixArt from '$lib/components/ui/MixArt.svelte';
	import CollectionHeader from '$lib/components/ui/CollectionHeader.svelte';
	import TrackTable from '$lib/components/ui/TrackTable.svelte';
	import TrackRow from '$lib/components/ui/TrackRow.svelte';
	import TrackMeta from '$lib/components/ui/TrackMeta.svelte';
	import TrackIndexCell from '$lib/components/ui/TrackIndexCell.svelte';
	import TrackTitleCell from '$lib/components/ui/TrackTitleCell.svelte';
	import TrackContextMenu, {
		contextMenuStateFor,
		type ContextMenuState
	} from '$lib/components/TrackContextMenu.svelte';
	import { mixItemKey, mixItemTarget } from '$lib/mixes';
	import {
		isTargetCurrent,
		queueItemForTarget,
		targetArtist,
		targetId,
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
	const isCurrentQueue = $derived(targets.some(isTargetCurrent));

	let contextMenu = $state<ContextMenuState | null>(null);

	function playFrom(index: number) {
		player.playOrToggle(queue, index);
	}

	function playAll() {
		if (queue.length === 0) return;
		if (isCurrentQueue) player.toggle();
		else player.playQueue(queue, 0);
	}

	function playShuffled() {
		if (queue.length === 0) return;
		player.playQueue(shuffle(queue), 0);
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
		meta="{items.length} {items.length === 1 ? 'canción' : 'canciones'} · {fmtTime(totalSeconds)}"
	>
		{#snippet cover()}
			<MixArt {items} class="h-36 w-36 shrink-0 rounded-[18px] shadow-cover-lg sm:h-41 sm:w-41" />
		{/snippet}
		{#snippet actions()}
			<Button size="sm" onclick={playAll} disabled={queue.length === 0}>
				{#if isCurrentQueue && player.playing}
					<Pause class="h-4 w-4" strokeWidth={1.5} />
					Pausar
				{:else}
					<Play class="h-4 w-4" strokeWidth={1.5} />
					Reproducir
				{/if}
			</Button>
			<Button variant="secondary" size="sm" onclick={playShuffled} disabled={queue.length === 0}>
				<Shuffle class="h-4 w-4" />
				Aleatorio
			</Button>
		{/snippet}
	</CollectionHeader>

	{#if form?.message}
		<Alert tone="danger" class="mt-4">{form.message}</Alert>
	{/if}

	<TrackTable index class="mt-6 sm:mt-8">
		{#each items as item, i (mixItemKey(item))}
			{@const active = isTargetCurrent(targets[i])}
			<TrackRow {active} oncontextmenu={(e) => (contextMenu = contextMenuStateFor(e, targets[i]))}>
				<TrackIndexCell index={i} {active} playing={player.playing} onToggle={() => playFrom(i)} />
				<TrackTitleCell
					trackId={targetId(targets[i])}
					title={targetTitle(targets[i])}
					artist={targetArtist(targets[i])}
					{active}
					onClick={() => playFrom(i)}
				/>
				<TrackMeta>{fmtTime(Number(item.durationSeconds))}</TrackMeta>
			</TrackRow>
		{/each}
	</TrackTable>
</Page>

{#if contextMenu}
	<TrackContextMenu
		menu={contextMenu}
		playlists={data.playlists}
		onClose={() => (contextMenu = null)}
		onAddToQueue={() => {
			const menu = contextMenu;
			if (menu) {
				const index = targets.findIndex((target) => targetId(target) === targetId(menu));
				if (index >= 0) player.addToQueue(queue[index]);
			}
			contextMenu = null;
		}}
	/>
{/if}
