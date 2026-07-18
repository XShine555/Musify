<script lang="ts">
	import Music from '@lucide/svelte/icons/music';
	import { player, toQueueItems, type QueueItem } from '$lib/player/player.svelte';
	import { hueFor } from '$lib/theme/color';
	import Cover from '$lib/components/ui/Cover.svelte';
	import PlaylistCard from '$lib/components/ui/PlaylistCard.svelte';
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
		type ContextMenuTarget,
		type ContextMenuState
	} from '$lib/components/TrackContextMenu.svelte';
	import type { YouTubeSong } from '$lib/types';
  import { appendUnique } from '$lib/collections.js';

	let { data } = $props();

	let youtubeFillerItems = $state<YouTubeSong[]>([]);
	let ytContinuation = $state('');
	let ytQuery = $state('');
	let contextMenu = $state<ContextMenuState | null>(null);

	type LocalNovedad = (typeof data.novedades)[number];

	let localNovedades = $state<LocalNovedad[]>([]);
	let localPage = $state(1);
	let localHasNext = $state(false);
	let loadingMore = $state(false);

	$effect(() => {
		youtubeFillerItems = [];
		ytContinuation = '';
		ytQuery = '';
		localNovedades = data.novedades;
		localPage = 1;
		localHasNext = data.novedadesHasNext;
		data.youtubeFiller.then((filler) => {
			youtubeFillerItems = filler.items;
			ytContinuation = filler.continuationToken;
			ytQuery = filler.query;
		});
	});

	const novedades = $derived([
		...localNovedades,
		...youtubeFillerItems.map((song) => ({ kind: 'youtube' as const, song }))
	]);

	const hasMoreNovedades = $derived(localHasNext || ytContinuation !== '');

	async function loadMoreNovedades() {
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
				localNovedades = appendUnique(
					localNovedades,
					next.items.map((track: LocalNovedad['track']) => ({
						kind: 'local' as const,
						track
					})),
					item => item.track.id
				);
				localPage = Number(next.pageNumber);
				localHasNext = Boolean(next.hasNextPage);
			} else if (ytContinuation && ytQuery) {
				const params = new URLSearchParams({ query: ytQuery, continuation: ytContinuation });
				const res = await fetch(`/api/youtube/search?${params}`);
				if (!res.ok) throw new Error(String(res.status));
				const next = (await res.json()) as { items: YouTubeSong[]; continuationToken: string };
				youtubeFillerItems = appendUnique(
					youtubeFillerItems,
					next.items,
					item => item.videoId
				);
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
	const trackIds = $derived(data.trackIds);

	function novedadId(item: (typeof novedades)[number]) {
		return item.kind === 'youtube' ? item.song.videoId : item.track.id;
	}

	function novedadTitle(item: (typeof novedades)[number]) {
		return item.kind === 'youtube' ? item.song.title : item.track.title;
	}

	function novedadArtist(item: (typeof novedades)[number]) {
		return item.kind === 'youtube' ? item.song.artist : item.track.artist;
	}

	function novedadExplicit(item: (typeof novedades)[number]) {
		return item.kind === 'youtube' ? item.song.isExplicit : false;
	}

	function novedadQueueItem(item: ContextMenuTarget): QueueItem {
		return item.kind === 'youtube'
			? {
					id: item.song.videoId,
					title: item.song.title,
					artist: item.song.artist,
					source: 'youtube',
					coverUrl: item.song.thumbnailUrl,
					explicit: item.song.isExplicit
				}
			: toQueueItems([item.track])[0];
	}

	function playNovedad(index: number) {
		player.playOrToggle(novedades.map(novedadQueueItem), index);
	}

	function openContextMenu(event: MouseEvent, target: ContextMenuTarget) {
		contextMenu = contextMenuStateFor(event, target);
	}

	function closeContextMenu() {
		contextMenu = null;
	}

	function addToQueue() {
		if (!contextMenu) return;
		player.addToQueue(novedadQueueItem(contextMenu));
		closeContextMenu();
	}

	function contextMenuTargetFor(item: QueueItem): ContextMenuTarget {
		return item.source === 'youtube'
			? {
					kind: 'youtube',
					song: {
						videoId: String(item.id),
						title: item.title,
						artist: item.artist ?? '',
						album: '',
						durationSeconds: 0,
						thumbnailUrl: item.coverUrl ?? '',
						isExplicit: item.explicit ?? false
					}
				}
			: {
					kind: 'local',
					track: {
						id: item.id,
						title: item.title,
						artist: item.artist,
						source: 'Local',
						isExplicit: item.explicit
					}
				};
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

	const greeting = (() => {
		const h = new Date().getHours();
		const pools =
			h < 6
				? ['¿Aún despierto?', 'La noche también tiene banda sonora', 'Un ratito más, con música']
				: h < 12
					? ['Buenos días', 'Que empiece bien la mañana', 'Un nuevo día, una nueva lista']
					: h < 14
						? ['Buenos días', 'El mediodía sabe mejor con música']
						: h < 19
							? ['Buenas tardes', 'La tarde pide su banda sonora', 'Un respiro, con algo de música']
							: h < 23
								? ['Buenas noches', 'Hora de bajar el ritmo', 'Que la noche te acompañe']
								: ['¿Aún despierto?', 'La noche también tiene banda sonora'];
		const pick = pools[Math.floor(Math.random() * pools.length)];
		return /[.!?]$/.test(pick) ? pick : `${pick}.`;
	})();

	function playRecent(index: number) {
		player.playOrToggle(recentlyPlayed, index);
	}
</script>

<svelte:head>
	<title>Musify</title>
	<meta name="description" content="Tu música, sin límites." />
</svelte:head>

<section class="page-x relative overflow-hidden pt-11 pb-16">
	<div
		class="pointer-events-none absolute inset-0 blur-[70px] saturate-150"
		style="background:linear-gradient(135deg, var(--mf-accent), color-mix(in oklch, var(--mf-accent), black 55%));animation:breathe 9s ease-in-out infinite"
	></div>
	<div class="animate-enter relative flex flex-col gap-2.5">
		<p class="tracking-[0.14em] text-fg-2 uppercase">{greeting}</p>
		<h1 class="max-w-160 text-5xl leading-[1.05] font-extrabold text-fg">
			Tu música. Sin límites.
		</h1>
	</div>
</section>

{#if recentlyPlayed.length > 0}
	<section class="page-x pt-6 pb-3">
		<SectionHeading title="Escuchado recientemente" />
		<MediaGrid min="300px">
			{#each recentlyPlayed as track, i (track.id)}
				<button
					type="button"
					onclick={() => playRecent(i)}
					oncontextmenu={(e) => openContextMenu(e, contextMenuTargetFor(track))}
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
						class="h-14 w-14 flex-shrink-0 rounded-control"
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

{#if playlists.length > 0}
	<section class="page-x pt-6 pb-3">
		<SectionHeading title="Mis listas" />
		<MediaGrid>
			{#each playlists as playlist, i (playlist.id)}
				<PlaylistCard
					id={playlist.id}
					name={playlist.name}
					description={playlist.description}
					trackIds={trackIds[playlist.id] ?? []}
					updatedAt={playlist.updatedAt}
					index={i}
				/>
			{/each}
		</MediaGrid>
	</section>
{/if}

<section class="page-x pt-6 pb-10">
	<SectionHeading title="Novedades" />
	{#if novedades.length > 0}
		<MediaGrid as="ul">
			{#each novedades as item, i (novedadId(item))}
				<TrackTile
					id={novedadId(item)}
					title={novedadTitle(item)}
					artist={novedadArtist(item)}
					coverSrc={item.kind === 'youtube' ? item.song.thumbnailUrl : undefined}
					hue={hueFor(novedadId(item))}
					explicit={novedadExplicit(item)}
					active={player.current.id === novedadId(item)}
					playing={player.isPlaying}
					index={i}
					onClick={() => playNovedad(i)}
					onContextMenu={(e) => openContextMenu(e, item)}
				/>
			{/each}
		</MediaGrid>
		<InfiniteScroll
			onLoadMore={loadMoreNovedades}
			hasMore={hasMoreNovedades}
			loading={loadingMore}
		/>
	{:else}
		<EmptyState icon={Music} title="Todavía no hay música" />
	{/if}
</section>

{#if contextMenu}
	<TrackContextMenu menu={contextMenu} {playlists} onClose={closeContextMenu} onAddToQueue={addToQueue} />
{/if}
