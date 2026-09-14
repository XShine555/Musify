<script lang="ts">
	import Heart from '@lucide/svelte/icons/heart';
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import X from '@lucide/svelte/icons/x';
	import { player, type QueueItem } from '$lib/player/player.svelte';
	import { liked } from '$lib/player/liked.svelte';
	import { fmtDate } from '$lib/format';
	import Page from '$lib/components/ui/Page.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import IconButton from '$lib/components/ui/IconButton.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import TrackTable from '$lib/components/ui/TrackTable.svelte';
	import TrackRow from '$lib/components/ui/TrackRow.svelte';
	import TrackMeta from '$lib/components/ui/TrackMeta.svelte';
	import TrackIndexCell from '$lib/components/ui/TrackIndexCell.svelte';
	import TrackTitleCell from '$lib/components/ui/TrackTitleCell.svelte';

	const tracks = $derived(liked.list);
	const queueItems = $derived<QueueItem[]>(
		tracks.map((t) => ({
			id: t.id,
			title: t.title,
			artist: t.artist,
			source: t.source,
			coverUrl: t.coverUrl,
			explicit: t.explicit
		}))
	);
	const isCurrentQueue = $derived(tracks.some((t) => t.id === player.current.id));

	function playFrom(index: number) {
		player.playOrToggle(queueItems, index);
	}

	function playAll() {
		if (queueItems.length === 0) return;
		if (isCurrentQueue) player.toggle();
		else player.playQueue(queueItems, 0);
	}
</script>

<svelte:head>
	<title>Me gusta</title>
	<meta name="description" content="Las canciones que te gustan en Musify." />
</svelte:head>

<Page>
	<div class="flex flex-col gap-5 sm:flex-row sm:items-center sm:gap-6">
		<div
			class="grid h-36 w-36 shrink-0 place-items-center rounded-[18px] shadow-[0_24px_56px_rgba(0,0,0,.55)] sm:h-41 sm:w-41"
			style="background:linear-gradient(150deg, oklch(0.4 0.09 330), oklch(0.18 0.04 10))"
		>
			<Heart class="h-10 w-10 text-white/90" fill="currentColor" strokeWidth={0} />
		</div>
		<div class="min-w-0 flex-1">
			<p class="text-[10.5px] font-semibold tracking-[0.16em] text-fg-2 uppercase">Colección</p>
			<h1
				class="mt-3 font-display text-[28px] font-semibold tracking-[-0.035em] text-fg sm:text-[33px]"
			>
				Me gusta
			</h1>
			<p class="mt-3.25 text-[12.5px] text-fg-2">
				{tracks.length}
				{tracks.length === 1 ? 'canción' : 'canciones'}
			</p>
		</div>
	</div>

	<div class="mt-6 flex flex-wrap items-center gap-2.5 sm:mt-7">
		<Button size="sm" onclick={playAll} disabled={tracks.length === 0}>
			{#if isCurrentQueue && player.playing}
				<Pause class="h-4 w-4" strokeWidth={1.5} />
				Pausar
			{:else}
				<Play class="h-4 w-4" strokeWidth={1.5} />
				Reproducir
			{/if}
		</Button>
	</div>

	{#if tracks.length > 0}
		<TrackTable
			index
			action
			meta={[
				{ label: 'Álbum', width: '160px' },
				{ label: 'Añadida', width: '140px' }
			]}
			class="mt-6 sm:mt-8"
		>
			{#each tracks as track, i (track.id)}
				{@const active = player.current.id === track.id}
				<TrackRow {active}>
					<TrackIndexCell
						index={i}
						{active}
						playing={player.playing}
						onToggle={() => playFrom(i)}
					/>
					<TrackTitleCell
						trackId={track.id}
						title={track.title}
						artist={track.artist}
						ownerUserId={track.ownerUserId}
						coverSrc={track.coverUrl}
						explicit={track.explicit}
						{active}
						onClick={() => playFrom(i)}
					/>
					<TrackMeta class="hidden sm:block">—</TrackMeta>
					<TrackMeta class="hidden sm:block">{fmtDate(track.likedAt)}</TrackMeta>
					<TrackMeta>—</TrackMeta>
					<IconButton
						label="Quitar de Me gusta"
						size="sm"
						revealOnHover
						onclick={() => liked.toggle(track)}
					>
						<X class="h-3.5 w-3.5" />
					</IconButton>
				</TrackRow>
			{/each}
		</TrackTable>
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
