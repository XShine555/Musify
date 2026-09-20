<script lang="ts">
	import Heart from '@lucide/svelte/icons/heart';
	import X from '@lucide/svelte/icons/x';
	import { player, type QueueItem } from '$lib/player/player.svelte';
	import { liked } from '$lib/player/liked.svelte';
	import { fmtDate, fmtTime } from '$lib/format';
	import Page from '$lib/components/ui/Page.svelte';
	import CollectionHeader from '$lib/components/ui/CollectionHeader.svelte';
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
	<CollectionHeader
		eyebrow="Colección"
		title="Me gusta"
		meta="{tracks.length} {tracks.length === 1 ? 'Canción' : 'Canciones'}"
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
		<TrackTable
			index
			action
			meta={[
				{ label: 'Álbum', width: '76px' },
				{ label: 'Añadida', width: '124px' },
				{ label: 'Escuchas', width: '106px' }
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
						explicit={track.explicit}
						{active}
						onClick={() => playFrom(i)}
					/>
					<TrackMeta class="hidden sm:block">—</TrackMeta>
					<TrackMeta class="hidden sm:block">{fmtDate(track.likedAt)}</TrackMeta>
					<TrackMeta class="hidden sm:block">{Number(track.listensCount ?? 0)}</TrackMeta>
					<TrackMeta>{fmtTime(Number(track.duration))}</TrackMeta>
					<IconButton
						label="Quitar de Me gusta"
						size="xs"
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
