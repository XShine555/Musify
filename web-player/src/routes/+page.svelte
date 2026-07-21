<script lang="ts">
	import Music from '@lucide/svelte/icons/music';
	import { player, toQueueItems, type QueueItem } from '$lib/player/player.svelte';
	import {
		isTargetCurrent,
		queueItemForTarget,
		targetArtist,
		targetCoverSrc,
		targetExplicit,
		targetForQueueItem,
		targetId,
		targetTitle,
		type TrackTarget
	} from '$lib/tracks';
	import { hueFor } from '$lib/theme/color';
	import Cover from '$lib/components/ui/Cover.svelte';
	import PlaylistCard from '$lib/components/ui/PlaylistCard.svelte';
	import MixCard from '$lib/components/ui/MixCard.svelte';
	import AlbumCard from '$lib/components/ui/AlbumCard.svelte';
	import NowPlaying from '$lib/components/ui/NowPlaying.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import ExplicitBadge from '$lib/components/ui/ExplicitBadge.svelte';
	import InfiniteScroll from '$lib/components/ui/InfiniteScroll.svelte';
	import { HOME_LATEST_PAGE_SIZE } from '$lib/config';
	import MediaGrid from '$lib/components/ui/MediaGrid.svelte';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';
	import TrackTile from '$lib/components/TrackTile.svelte';
	import TrackContextMenu, {
		contextMenuStateFor,
		type ContextMenuState
	} from '$lib/components/TrackContextMenu.svelte';
	import type { YouTubeSong } from '$lib/types';
	import { appendUnique } from '$lib/collections';

	let { data } = $props();

	let youtubeFillerItems = $state<YouTubeSong[]>([]);
	let ytContinuation = $state('');
	let ytQuery = $state('');
	let contextMenu = $state<ContextMenuState | null>(null);

	type LocalLatest = (typeof data.latest)[number];

	let localLatest = $state<LocalLatest[]>([]);
	let localPage = $state(1);
	let localHasNext = $state(false);
	let loadingMore = $state(false);

	$effect(() => {
		youtubeFillerItems = [];
		ytContinuation = '';
		ytQuery = '';
		localLatest = data.latest;
		localPage = 1;
		localHasNext = data.latestHasNext;

		let cancelled = false;
		data.youtubeFiller.then((filler) => {
			if (cancelled) return;
			youtubeFillerItems = filler.items;
			ytContinuation = filler.continuationToken;
			ytQuery = filler.query;
		});
		return () => {
			cancelled = true;
		};
	});

	const latest = $derived<TrackTarget[]>([
		...localLatest,
		...youtubeFillerItems.map((song) => ({ kind: 'youtube' as const, song }))
	]);

	const hasMoreLatest = $derived(localHasNext || ytContinuation !== '');

	async function loadMoreLatest() {
		if (loadingMore) return;
		loadingMore = true;
		try {
			if (localHasNext) {
				const params = new URLSearchParams({
					pageNumber: String(localPage + 1),
					pageSize: String(HOME_LATEST_PAGE_SIZE)
				});
				const res = await fetch(`/api/tracks?${params}`);
				if (!res.ok) throw new Error(String(res.status));
				const next = await res.json();
				localLatest = appendUnique(
					localLatest,
					next.items.map((track: LocalLatest['track']) => ({ kind: 'local' as const, track })),
					(item) => item.track.id
				);
				localPage = Number(next.pageNumber);
				localHasNext = Boolean(next.hasNextPage);
			} else if (ytContinuation && ytQuery) {
				const params = new URLSearchParams({ query: ytQuery, continuation: ytContinuation });
				const res = await fetch(`/api/youtube/search?${params}`);
				if (!res.ok) throw new Error(String(res.status));
				const next = (await res.json()) as { items: YouTubeSong[]; continuationToken: string };
				youtubeFillerItems = appendUnique(youtubeFillerItems, next.items, (i) => i.videoId);
				ytContinuation = next.continuationToken;
			}
		} catch {
			if (localHasNext) localHasNext = false;
			else ytContinuation = '';
		} finally {
			loadingMore = false;
		}
	}
	const playlists = $derived(data.playlists);
	const albums = $derived(data.albums);
	const mixes = $derived(data.mixes);

	function playLatest(index: number) {
		player.playOrToggle(latest.map(queueItemForTarget), index);
	}

	function openContextMenu(event: MouseEvent, target: TrackTarget) {
		contextMenu = contextMenuStateFor(event, target);
	}

	function closeContextMenu() {
		contextMenu = null;
	}

	function addToQueue() {
		if (!contextMenu) return;
		player.addToQueue(queueItemForTarget(contextMenu));
		closeContextMenu();
	}

	const recentlyPlayed = $derived.by(() => {
		const seen = new Set<string>();
		const merged: QueueItem[] = [];
		for (const t of player.recentlyPlayed) {
			const id = String(t.id);
			if (seen.has(id)) continue;
			seen.add(id);
			merged.push({
				id,
				title: t.title,
				artist: t.artist,
				source: t.source,
				coverUrl: t.coverUrl,
				explicit: t.explicit
			});
		}
		for (const item of toQueueItems(data.recentlyPlayed)) {
			const id = String(item.id);
			if (seen.has(id)) continue;
			seen.add(id);
			merged.push({
				id,
				title: item.title,
				artist: item.artist,
				source: item.source ?? 'local',
				coverUrl: item.coverUrl,
				explicit: item.explicit
			});
		}
		return merged.slice(0, 15);
	});

	function playRecent(index: number) {
		player.playOrToggle(recentlyPlayed, index);
	}
</script>

<svelte:head>
	<title>Musify</title>
	<meta name="description" content="Tu música, sin límites." />
</svelte:head>

<section class="relative overflow-hidden page-x pt-8 pb-10 sm:pt-11 sm:pb-16">
	<div
		class="pointer-events-none absolute inset-0 blur-[70px] saturate-150"
		style="background:linear-gradient(135deg, var(--color-accent), color-mix(in oklch, var(--color-accent), black 55%));animation:breathe 9s ease-in-out infinite"
	></div>
	<div class="animate-enter relative flex flex-col gap-2 sm:gap-2.5">
		<p class="text-sm tracking-[0.14em] text-fg-2 uppercase sm:text-base">{data.greeting}</p>
		<h1 class="max-w-160 text-3xl leading-[1.05] font-extrabold text-fg sm:text-5xl">
			Tu música. Sin límites.
		</h1>
	</div>
</section>

{#if recentlyPlayed.length > 0}
	<section class="page-x pt-5 pb-3 sm:pt-6">
		<SectionHeading title="Volver a escuchar" />
		<MediaGrid min="280px" minMobile="220px">
			{#each recentlyPlayed as track, i (track.id)}
				<button
					type="button"
					onclick={() => playRecent(i)}
					oncontextmenu={(e) => openContextMenu(e, targetForQueueItem(track))}
					aria-label="{player.current.id === track.id && player.playing
						? 'Pausar'
						: 'Reproducir'} {track.title}"
					class="flex min-w-0 items-center gap-3 rounded-control bg-surface p-2 text-left transition hover:bg-surface-hover"
				>
					<Cover
						trackId={track.id}
						src={track.coverUrl}
						hue={hueFor(track.id)}
						size="small"
						alt={track.title}
						class="h-14 w-14 shrink-0 rounded-control"
					>
						{#if player.current.id === track.id}
							<NowPlaying paused={!player.playing} />
						{/if}
					</Cover>
					<div class="min-w-0">
						<div class="flex min-w-0 items-center gap-1.5">
							{#if track.explicit}
								<ExplicitBadge />
							{/if}
							<span class="truncate text-fg">{track.title}</span>
						</div>
						{#if track.artist}
							<div class="truncate text-sm text-muted">{track.artist}</div>
						{/if}
					</div>
				</button>
			{/each}
		</MediaGrid>
	</section>
{/if}

{#if mixes.length > 0 || playlists.length > 0}
	<section class="grid gap-x-8 gap-y-5 page-x pt-5 pb-3 sm:pt-6 lg:grid-cols-2">
		{#if mixes.length > 0}
			<div class="min-w-0">
				<SectionHeading title="Hechas para ti" />
				<MediaGrid min="150px" minMobile="130px">
					{#each mixes as mix, i (mix.id)}
						<MixCard {mix} index={i} />
					{/each}
				</MediaGrid>
			</div>
		{/if}

		{#if playlists.length > 0}
			<div class="min-w-0">
				<SectionHeading title="Mis listas">
					{#snippet actions()}
						{#if data.playlistsHasMore}
							<a href="/playlists" class="text-sm text-fg-3 transition hover:text-accent-soft">
								Ver todas
							</a>
						{/if}
					{/snippet}
				</SectionHeading>
				<MediaGrid min="150px" minMobile="130px">
					{#each playlists as playlist, i (playlist.id)}
						<PlaylistCard
							id={playlist.id}
							name={playlist.name}
							description={playlist.description}
							trackIds={playlist.coverTrackIds}
							updatedAt={playlist.updatedAt}
							index={i}
						/>
					{/each}
				</MediaGrid>
			</div>
		{/if}
	</section>
{/if}

{#if albums.length > 0}
	<section class="page-x pt-5 pb-3 sm:pt-6">
		<SectionHeading title="Álbumes escuchados recientemente">
			{#snippet actions()}
				<a href="/albums" class="text-sm text-fg-3 transition hover:text-accent-soft">Ver todos</a>
			{/snippet}
		</SectionHeading>
		<MediaGrid>
			{#each albums as album, i (album.id)}
				<AlbumCard
					id={album.id}
					title={album.title}
					releaseYear={album.releaseYear === null ? undefined : Number(album.releaseYear)}
					trackCount={Number(album.trackCount)}
					trackIds={album.coverTrackIds}
					index={i}
				/>
			{/each}
		</MediaGrid>
	</section>
{/if}

<section class="page-x pt-5 pb-10 sm:pt-6">
	<SectionHeading title="Descubrir" />
	{#if latest.length > 0}
		<MediaGrid as="ul">
			{#each latest as item, i (targetId(item))}
				<TrackTile
					id={targetId(item)}
					title={targetTitle(item)}
					artist={targetArtist(item)}
					coverSrc={targetCoverSrc(item)}
					hue={hueFor(targetId(item))}
					explicit={targetExplicit(item)}
					active={isTargetCurrent(item)}
					playing={player.isPlaying}
					index={i}
					onClick={() => playLatest(i)}
					onContextMenu={(e) => openContextMenu(e, item)}
				/>
			{/each}
		</MediaGrid>
		<InfiniteScroll onLoadMore={loadMoreLatest} hasMore={hasMoreLatest} loading={loadingMore} />
	{:else}
		<EmptyState icon={Music} title="Todavía no hay música" />
	{/if}
</section>

{#if contextMenu}
	<TrackContextMenu
		menu={contextMenu}
		{playlists}
		onClose={closeContextMenu}
		onAddToQueue={addToQueue}
	/>
{/if}
