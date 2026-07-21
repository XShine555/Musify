<script lang="ts">
	import ArrowLeft from '@lucide/svelte/icons/arrow-left';
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import Shuffle from '@lucide/svelte/icons/shuffle';
	import { player } from '$lib/player/player.svelte';
	import { hueFor } from '$lib/theme/color';
	import { fmtTime } from '$lib/format';
	import { shuffle } from '$lib/collections';
	import Page from '$lib/components/ui/Page.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import MixArt from '$lib/components/ui/MixArt.svelte';
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
		targetCoverSrc,
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
	<title>{mix.title} · Musify</title>
	<meta name="description" content="Mezcla {mix.title}, hecha para ti." />
</svelte:head>

<Page>
	<a
		href="/"
		class="group inline-flex items-center text-fg-3 transition-colors hover:text-accent-soft"
	>
		<span class="grid h-8 w-8 place-items-center transition-colors group-hover:text-accent-soft">
			<ArrowLeft class="h-4 w-4" />
		</span>
		Volver al inicio
	</a>

	<div class="mt-5 flex flex-col gap-5 sm:mt-6 sm:flex-row sm:items-end sm:gap-6">
		<MixArt
			{items}
			hue={hueFor(mix.id)}
			class="h-36 w-36 shrink-0 rounded-art-lg shadow-art-lg sm:h-44 sm:w-44"
		/>
		<div class="min-w-0 flex-1">
			<p class="text-sm tracking-[0.14em] text-fg-3 uppercase">Mezcla</p>
			<h1
				class="mt-1.5 text-3xl font-semibold tracking-tight break-words text-fg sm:text-5xl md:text-7xl"
			>
				{mix.title}
			</h1>
			{#if mix.subtitle}
				<p class="mt-3 text-base text-fg-2">{mix.subtitle}</p>
			{/if}
			<p class="mt-1 text-sm text-muted">
				{items.length}
				{items.length === 1 ? 'canción' : 'canciones'} · {fmtTime(totalSeconds)}
			</p>
		</div>
	</div>

	<div class="mt-6 flex flex-wrap items-center gap-2.5 sm:mt-7">
		<Button size="sm" onclick={playAll} disabled={queue.length === 0}>
			{#if isCurrentQueue && player.playing}
				<Pause class="h-4 w-4" fill="currentColor" strokeWidth={1.5} />
				Pausar
			{:else}
				<Play class="h-4 w-4" fill="currentColor" strokeWidth={1.5} />
				Reproducir
			{/if}
		</Button>
		<Button variant="secondary" size="sm" onclick={playShuffled} disabled={queue.length === 0}>
			<Shuffle class="h-4 w-4" />
			Aleatorio
		</Button>
	</div>

	{#if form?.message}
		<Alert tone="danger" class="mt-4">{form.message}</Alert>
	{/if}

	<TrackTable index class="mt-6 sm:mt-8">
		{#each items as item, i (mixItemKey(item))}
			<TrackRow oncontextmenu={(e) => (contextMenu = contextMenuStateFor(e, targets[i]))}>
				<TrackIndexCell
					index={i}
					active={isTargetCurrent(targets[i])}
					playing={player.playing}
					onToggle={() => playFrom(i)}
				/>
				<TrackTitleCell
					trackId={targetId(targets[i])}
					title={targetTitle(targets[i])}
					artist={targetArtist(targets[i])}
					coverSrc={targetCoverSrc(targets[i])}
					hue={hueFor(mixItemKey(item))}
					explicit={item.isExplicit}
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
