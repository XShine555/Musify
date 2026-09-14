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
	<title>{mix.title}</title>
	<meta name="description" content="Mezcla {mix.title}, hecha para ti." />
</svelte:head>

<Page>
	<BackLink href="/" label="Volver al inicio" />

	<div class="mt-3 flex flex-col gap-5 sm:mt-4 sm:flex-row sm:items-end sm:gap-6">
		<MixArt
			{items}
			class="h-36 w-36 shrink-0 rounded-[18px] shadow-[0_24px_56px_rgba(0,0,0,.55)] sm:h-41 sm:w-41"
		/>
		<div class="min-w-0 flex-1">
			<p class="text-[10.5px] font-semibold tracking-[0.16em] text-fg-2 uppercase">Mezcla</p>
			<h1
				class="mt-1.5 font-display text-[28px] font-semibold tracking-[-0.035em] break-words text-fg sm:text-[33px]"
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
	</div>

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
					coverSrc={targetCoverSrc(targets[i])}
					explicit={item.isExplicit}
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
