<script lang="ts">
	import ArrowRight from '@lucide/svelte/icons/arrow-right';
	import ChevronLeft from '@lucide/svelte/icons/chevron-left';
	import ChevronRight from '@lucide/svelte/icons/chevron-right';
	import MoreHorizontal from '@lucide/svelte/icons/more-horizontal';
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import { player, toQueueItems, type QueueItem } from '$lib/player/player.svelte';
	import {
		isTargetCurrent,
		queueItemForTarget,
		targetArtist,
		targetCoverSrc,
		targetDurationSeconds,
		targetExplicit,
		targetForQueueItem,
		targetId,
		targetTitle,
		type TrackTarget
	} from '$lib/tracks';
	import { fmtTime } from '$lib/format';
	import Cover from '$lib/components/ui/Cover.svelte';
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
	import MixBentoTile from '$lib/components/ui/MixBentoTile.svelte';
	import Rail from '$lib/components/ui/Rail.svelte';
	import NowPlaying from '$lib/components/ui/NowPlaying.svelte';
	import ExplicitBadge from '$lib/components/ui/ExplicitBadge.svelte';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';
	import TrackContextMenu, {
		contextMenuStateFor,
		type ContextMenuState
	} from '$lib/components/TrackContextMenu.svelte';
	import AlbumContextMenu, {
		albumContextMenuStateFor,
		type AlbumMenuState
	} from '$lib/components/AlbumContextMenu.svelte';
	import { fetchAlbumQueueItems } from '$lib/albums';
	import type { YouTubeAlbumResult, YouTubeSong } from '$lib/types';
	import { appendUnique } from '$lib/collections';
	import { HOME_LATEST_PAGE_SIZE, HOME_POPULAR_MAX } from '$lib/config';

	let { data } = $props();

	let contextMenu = $state<ContextMenuState | null>(null);
	let albumMenu = $state<AlbumMenuState | null>(null);
	let topMusicFillerItems = $state<YouTubeSong[]>([]);
	let youtubeFillerItems = $state<YouTubeSong[]>([]);
	let popularFillerItems = $state<YouTubeSong[]>([]);
	let albumFillerItems = $state<YouTubeAlbumResult[]>([]);
	let topMusicRail: ReturnType<typeof Rail> | undefined = $state();
	let recommendedRail: ReturnType<typeof Rail> | undefined = $state();

	$effect(() => {
		topMusicFillerItems = [];
		let cancelled = false;
		data.topMusicFiller.then((filler) => {
			if (cancelled) return;
			topMusicFillerItems = appendUnique([], filler.items, (song) => song.videoId);
		});
		return () => {
			cancelled = true;
		};
	});

	$effect(() => {
		youtubeFillerItems = [];
		let cancelled = false;
		data.youtubeFiller.then((filler) => {
			if (cancelled) return;
			youtubeFillerItems = appendUnique([], filler.items, (song) => song.videoId);
		});
		return () => {
			cancelled = true;
		};
	});

	$effect(() => {
		popularFillerItems = [];
		let cancelled = false;
		data.popularFiller.then((filler) => {
			if (cancelled) return;
			popularFillerItems = appendUnique([], filler.items, (song) => song.videoId);
		});
		return () => {
			cancelled = true;
		};
	});

	$effect(() => {
		albumFillerItems = [];
		let cancelled = false;
		data.albumFiller.then((filler) => {
			if (cancelled) return;
			albumFillerItems = appendUnique([], filler.items, (album) => album.albumId);
		});
		return () => {
			cancelled = true;
		};
	});

	const albums = $derived(data.albums);
	const mixes = $derived(data.mixes);
	const playlists = $derived(data.playlists);

	const newTargets = $derived<TrackTarget[]>(
		data.newReleases.map((track) => ({ kind: 'local' as const, track }))
	);
	const topMusicTargets = $derived<TrackTarget[]>([
		...newTargets,
		...topMusicFillerItems.map((song) => ({ kind: 'youtube' as const, song }))
	]);
	const recTargets = $derived<TrackTarget[]>(
		youtubeFillerItems.map((song) => ({ kind: 'youtube' as const, song }))
	);

	type RecommendedAlbum =
		| { kind: 'recent'; album: (typeof albums)[number] }
		| { kind: 'discover'; album: YouTubeAlbumResult };

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
		return merged.slice(0, HOME_LATEST_PAGE_SIZE);
	});

	const popularTargets = $derived<TrackTarget[]>(
		[
			...recentlyPlayed.map(targetForQueueItem),
			...popularFillerItems.map((song) => ({ kind: 'youtube' as const, song }))
		].slice(0, HOME_POPULAR_MAX)
	);

	const recommendedAlbums = $derived<RecommendedAlbum[]>([
		...albums.map((album) => ({ kind: 'recent' as const, album })),
		...albumFillerItems.map((album) => ({ kind: 'discover' as const, album }))
	]);

	const heroTrack = $derived(recentlyPlayed[0] ?? null);
	const heroPlaying = $derived(
		heroTrack !== null && player.current.id === heroTrack.id && player.playing
	);

	function playRecent(index: number) {
		player.playOrToggle(recentlyPlayed, index);
	}
	function playTopMusic(index: number) {
		player.playOrToggle(topMusicTargets.map(queueItemForTarget), index);
	}
	function playPopular(index: number) {
		player.playOrToggle(popularTargets.map(queueItemForTarget), index);
	}
	function playRecommended(index: number) {
		player.playOrToggle(recTargets.map(queueItemForTarget), index);
	}

	function openContextMenu(event: MouseEvent, target: TrackTarget) {
		contextMenu = contextMenuStateFor(event, target);
	}
	function addToQueue() {
		if (!contextMenu) return;
		player.addToQueue(queueItemForTarget(contextMenu));
		contextMenu = null;
	}

	function openAlbumMenu(event: MouseEvent, kind: 'local' | 'youtube', albumId: string) {
		albumMenu = albumContextMenuStateFor(event, kind, albumId);
	}

	async function albumPlayNext() {
		if (!albumMenu) return;
		const { kind, albumId } = albumMenu;
		albumMenu = null;
		player.playNext(await fetchAlbumQueueItems(kind, albumId));
	}

	async function albumAddToQueue() {
		if (!albumMenu) return;
		const { kind, albumId } = albumMenu;
		albumMenu = null;
		player.appendToQueue(await fetchAlbumQueueItems(kind, albumId));
	}

	function albumCaption(album: (typeof albums)[number]) {
		return [
			album.releaseYear ? String(album.releaseYear) : undefined,
			`${Number(album.trackCount)} ${Number(album.trackCount) === 1 ? 'canción' : 'canciones'}`
		]
			.filter(Boolean)
			.join(' · ');
	}

	function youtubeAlbumCaption(album: YouTubeAlbumResult) {
		return [album.artist, album.releaseYear ? String(album.releaseYear) : undefined]
			.filter(Boolean)
			.join(' · ');
	}
</script>

<svelte:head>
	<title>Musify</title>
	<meta name="description" content="Tu música, sin límites." />
</svelte:head>

{#snippet railArrows(rail: ReturnType<typeof Rail> | undefined)}
	<div class="flex shrink-0 gap-2">
		<button
			type="button"
			onclick={() => rail?.scrollByPage(-1)}
			aria-label="Anterior"
			class="grid h-8 w-8 place-items-center rounded-full border border-line text-fg-2 transition hover:border-accent-soft/50 hover:text-accent-soft"
		>
			<ChevronLeft class="h-4 w-4" />
		</button>
		<button
			type="button"
			onclick={() => rail?.scrollByPage(1)}
			aria-label="Siguiente"
			class="grid h-8 w-8 place-items-center rounded-full border border-line text-fg-2 transition hover:border-accent-soft/50 hover:text-accent-soft"
		>
			<ChevronRight class="h-4 w-4" />
		</button>
	</div>
{/snippet}

<section class="relative overflow-hidden page-x pt-8 pb-8 sm:pt-10 sm:pb-10">
	<div class="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
		<div
			class="absolute inset-x-0 top-[-19rem] h-[34rem] sm:h-[42rem]"
			style="background:radial-gradient(58% 100% at 50% 0%, color-mix(in oklch, var(--color-accent), transparent 5%), color-mix(in oklch, var(--color-accent), transparent 48%) 42%, transparent 78%); animation:breathe 14s ease-in-out infinite"
		></div>
		<div class="absolute inset-x-0 bottom-0 h-16 bg-linear-to-b from-transparent to-bg"></div>
	</div>

	<div class="relative flex flex-col gap-10 lg:flex-row lg:items-end lg:justify-between lg:gap-8">
		<div class="animate-enter flex max-w-2xl flex-col gap-4">
			<p class="text-xs font-semibold tracking-[0.28em] text-fg-2 uppercase sm:text-sm">
				{data.greeting}
			</p>
			<h1
				class="font-display text-[clamp(3rem,7.5vw,5.5rem)] leading-[0.98] font-medium tracking-tight text-fg"
			>
				Tu música,<br /><span class="text-accent-soft">sin límites.</span>
			</h1>
			<p class="max-w-md text-base text-fg-2 sm:text-lg">
				Sube lo tuyo, descubre lo nuevo y escúchalo todo en un solo sitio.
			</p>
		</div>

		{#if heroTrack}
			<button
				type="button"
				onclick={() => playRecent(0)}
				aria-label="{heroPlaying ? 'Pausar' : 'Reproducir'} {heroTrack.title}"
				class="group/hero animate-enter relative flex w-full max-w-sm shrink-0 items-center gap-4 overflow-hidden rounded-panel border border-line-strong bg-surface/70 p-4 text-left backdrop-blur-xl transition duration-300 hover:border-accent-soft/50 hover:bg-surface-hover hover:shadow-glow sm:p-5"
				style="animation-delay:140ms"
			>
				<Cover
					trackId={heroTrack.id}
					src={heroTrack.coverUrl}
					size="medium"
					alt={heroTrack.title}
					class="h-16 w-16 shrink-0 rounded-art shadow-art sm:h-20 sm:w-20"
				>
					{#if player.current.id === heroTrack.id}
						<NowPlaying paused={!player.playing} />
					{/if}
				</Cover>
				<div class="min-w-0 flex-1">
					<p class="text-[0.65rem] font-semibold tracking-[0.2em] text-fg-2 uppercase">
						Seguir escuchando
					</p>
					<p class="mt-1 truncate font-display text-lg text-fg">{heroTrack.title}</p>
					{#if heroTrack.artist}
						<p class="truncate text-sm text-fg-2">{heroTrack.artist}</p>
					{/if}
				</div>
				<span
					class="grid h-11 w-11 shrink-0 place-items-center rounded-full bg-accent-soft text-on-accent shadow-art transition duration-300 group-hover/hero:scale-105"
				>
					{#if heroPlaying}
						<Pause class="h-4.5 w-4.5" fill="currentColor" />
					{:else}
						<Play class="h-4.5 w-4.5 translate-x-0.5" fill="currentColor" />
					{/if}
				</span>
			</button>
		{/if}
	</div>
</section>

<div class="flex flex-col gap-12 page-x pb-8 sm:pb-10">
	{#if topMusicTargets.length > 0}
		<section>
			<SectionHeading title="Top música">
				{#snippet actions()}
					{@render railArrows(topMusicRail)}
				{/snippet}
			</SectionHeading>
			<Rail bind:this={topMusicRail}>
				{#each topMusicTargets as target, i (targetId(target))}
					<button
						type="button"
						onclick={() => playTopMusic(i)}
						oncontextmenu={(e) => openContextMenu(e, target)}
						aria-label="Reproducir {targetTitle(target)}"
						class="group/card animate-enter block w-36 shrink-0 rounded-art text-left focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg focus-visible:outline-none"
						style="animation-delay:{Math.min(i, 10) * 45}ms"
					>
						<div class="relative h-36 w-36">
							<Cover
								trackId={targetId(target)}
								src={targetCoverSrc(target)}
								size="large"
								alt={targetTitle(target)}
								class="h-full w-full rounded-art shadow-art ring-1 ring-line transition duration-300 ease-out ring-inset group-hover/card:shadow-art-lg"
							>
								{#if isTargetCurrent(target)}
									<NowPlaying paused={!player.playing} />
								{/if}
							</Cover>
						</div>
						<div class="mt-3 flex min-w-0 items-center gap-1.5">
							{#if targetExplicit(target)}
								<ExplicitBadge />
							{/if}
							<span class="truncate text-fg transition-colors group-hover/card:text-accent-soft">
								{targetTitle(target)}
							</span>
						</div>
						{#if targetArtist(target)}
							<div class="mt-0.5 truncate text-sm text-fg-2">{targetArtist(target)}</div>
						{/if}
					</button>
				{/each}
			</Rail>
		</section>
	{/if}

	<div class="grid grid-cols-1 gap-10 lg:grid-cols-2 lg:gap-8">
		<section class="min-w-0">
			<SectionHeading title="Popular" />
			<div class="flex flex-col gap-1.5">
				{#each popularTargets as target, i (targetId(target))}
					{@const isCurrent = isTargetCurrent(target)}
					<div
						class="group/row flex min-w-0 items-center gap-3 rounded-control p-2 transition duration-200 {isCurrent
							? 'bg-surface-hover'
							: 'hover:bg-hover'}"
					>
						<button
							type="button"
							onclick={() => playPopular(i)}
							aria-label="{isCurrent && player.playing ? 'Pausar' : 'Reproducir'} {targetTitle(
								target
							)}"
							class="relative shrink-0"
						>
							<Cover
								trackId={targetId(target)}
								src={targetCoverSrc(target)}
								size="small"
								alt={targetTitle(target)}
								class="h-11 w-11 rounded-control shadow-art transition duration-200"
							>
								{#if isCurrent}
									<NowPlaying paused={!player.playing} />
								{/if}
							</Cover>
							{#if !isCurrent}
								<span
									class="absolute inset-0 grid place-items-center rounded-control bg-scrim/0 opacity-0 transition duration-200 group-hover/row:bg-scrim/50 group-hover/row:opacity-100"
								>
									<Play class="h-3.5 w-3.5 translate-x-0.5 text-on-art" fill="currentColor" />
								</span>
							{/if}
						</button>
						<button type="button" onclick={() => playPopular(i)} class="min-w-0 flex-1 text-left">
							<div class="flex min-w-0 items-center gap-1.5">
								{#if targetExplicit(target)}
									<ExplicitBadge />
								{/if}
								<span
									class="truncate text-sm text-fg transition-colors group-hover/row:text-accent-soft"
								>
									{targetTitle(target)}
								</span>
							</div>
							{#if targetArtist(target)}
								<div class="truncate text-xs text-fg-2">{targetArtist(target)}</div>
							{/if}
						</button>
						<button
							type="button"
							onclick={(e) => openContextMenu(e, target)}
							aria-label="Más opciones"
							class="shrink-0 rounded-full p-1.5 text-fg-2 opacity-0 transition group-hover/row:opacity-100 hover:text-fg"
						>
							<MoreHorizontal class="h-4 w-4" />
						</button>
					</div>
				{/each}
			</div>
		</section>

		<section class="min-w-0">
			<SectionHeading title="Álbumes recomendados">
				{#snippet actions()}
					{@render railArrows(recommendedRail)}
				{/snippet}
			</SectionHeading>
			<Rail bind:this={recommendedRail}>
				{#each recommendedAlbums as entry, i (entry.kind === 'recent' ? entry.album.id : entry.album.albumId)}
					{#if entry.kind === 'recent'}
						{@const album = entry.album}
						<a
							href={album.source === 'YouTube'
								? `/albums/external/youtube/${album.externalId}`
								: `/albums/${album.id}`}
							class="group/card animate-enter block w-36 shrink-0 text-center"
							style="animation-delay:{Math.min(i, 10) * 45}ms"
							oncontextmenu={(e) =>
								openAlbumMenu(e, album.source === 'YouTube' ? 'youtube' : 'local', album.id)}
						>
							{#if album.source === 'YouTube' && album.thumbnailUrl}
								<Cover
									trackId={album.id}
									src={album.thumbnailUrl}
									size="large"
									alt={album.title}
									class="mx-auto h-36 w-36 rounded-art shadow-art ring-1 ring-line transition duration-300 ease-out ring-inset group-hover/card:shadow-art-lg"
								/>
							{:else}
								<PlaylistArt
									trackIds={album.coverTrackIds}
									class="mx-auto h-36 w-36 rounded-art shadow-art ring-1 ring-line transition duration-300 ease-out ring-inset group-hover/card:shadow-art-lg"
								/>
							{/if}
							<div
								class="mt-3.5 truncate text-fg transition-colors group-hover/card:text-accent-soft"
							>
								{album.title}
							</div>
							<div class="mt-0.5 truncate text-sm text-fg-2">{albumCaption(album)}</div>
						</a>
					{:else}
						{@const album = entry.album}
						<a
							href="/albums/external/youtube/{album.albumId}"
							class="group/card animate-enter block w-36 shrink-0 text-center"
							style="animation-delay:{Math.min(i, 10) * 45}ms"
							oncontextmenu={(e) => openAlbumMenu(e, 'youtube', album.albumId)}
						>
							<Cover
								trackId={album.albumId}
								src={album.thumbnailUrl}
								size="large"
								alt={album.title}
								class="mx-auto h-36 w-36 rounded-art shadow-art ring-1 ring-line transition duration-300 ease-out ring-inset group-hover/card:shadow-art-lg"
							/>
							<div
								class="mt-3.5 truncate text-fg transition-colors group-hover/card:text-accent-soft"
							>
								{album.title}
							</div>
							<div class="mt-0.5 truncate text-sm text-fg-2">{youtubeAlbumCaption(album)}</div>
						</a>
					{/if}
				{/each}
			</Rail>
		</section>
	</div>

	{#if mixes.length > 0}
		<section>
			<SectionHeading title="Mezclas" />
			<div class="grid grid-cols-2 gap-3 sm:auto-rows-25 sm:grid-cols-4 sm:gap-4">
				{#each mixes as mix, i (mix.id)}
					<MixBentoTile
						{mix}
						index={i}
						featured={i === 0}
						banner={i === 4}
						class={i === 0
							? 'col-span-2 min-h-42 sm:row-span-2 sm:min-h-0'
							: i === 1
								? 'col-span-2 min-h-30 sm:min-h-0'
								: i === 4
									? 'col-span-2 min-h-24 sm:col-span-4 sm:min-h-0'
									: 'min-h-30 sm:min-h-0'}
					/>
				{/each}
			</div>
		</section>
	{/if}

	{#if playlists.length > 0}
		<section>
			<SectionHeading title="Listas de reproducción" />
			<Rail>
				{#each playlists as playlist, i (playlist.id)}
					<a
						href="/playlists/{playlist.id}"
						class="group/card animate-enter block w-40 shrink-0 pr-2 pb-2"
						style="animation-delay:{Math.min(i, 10) * 45}ms"
					>
						<div class="relative h-40 w-40">
							<div
								class="absolute inset-0 translate-x-2 translate-y-2 rounded-art bg-surface-2 ring-1 ring-line"
							></div>
							<div
								class="absolute inset-0 translate-x-1 translate-y-1 rounded-art bg-surface-hover ring-1 ring-line"
							></div>
							<PlaylistArt
								playlistId={playlist.id}
								trackIds={playlist.coverTrackIds}
								version={playlist.updatedAt}
								class="absolute inset-0 h-full w-full rounded-art shadow-art-lg ring-1 ring-line-strong"
							/>
						</div>
						<div
							class="mt-3.5 truncate text-fg transition-colors group-hover/card:text-accent-soft"
						>
							{playlist.name}
						</div>
						{#if playlist.description}
							<div class="mt-0.5 truncate text-sm text-fg-2">{playlist.description}</div>
						{/if}
					</a>
				{/each}
				<a
					href="/playlists"
					class="group/more grid h-40 w-40 shrink-0 place-items-center rounded-art border border-dashed border-line text-fg-2 transition duration-300 hover:border-accent-soft/50 hover:text-accent-soft"
				>
					<div class="flex flex-col items-center gap-2">
						<span
							class="grid h-9 w-9 place-items-center rounded-full bg-surface-2 transition-transform duration-300 group-hover/more:translate-x-0.5"
						>
							<ArrowRight class="h-4 w-4" />
						</span>
						<span class="text-sm">Ver todas</span>
					</div>
				</a>
			</Rail>
		</section>
	{/if}

	{#if recTargets.length > 0}
		<section>
			<SectionHeading title="Quizá te guste" />
			<ul class="grid grid-cols-1 gap-1 lg:grid-cols-2 lg:gap-x-8">
				{#each recTargets as target, i (targetId(target))}
					<li>
						<button
							type="button"
							onclick={() => playRecommended(i)}
							oncontextmenu={(e) => openContextMenu(e, target)}
							aria-label="Reproducir {targetTitle(target)}"
							class="group/row flex w-full min-w-0 items-center gap-3.5 rounded-control p-2 pr-4 text-left transition hover:bg-hover focus-visible:ring-2 focus-visible:ring-accent focus-visible:outline-none"
						>
							<Cover
								trackId={targetId(target)}
								src={targetCoverSrc(target)}
								size="small"
								alt={targetTitle(target)}
								class="h-11 w-11 shrink-0 rounded-control shadow-art ring-1 ring-line ring-inset"
							>
								{#if isTargetCurrent(target)}
									<NowPlaying paused={!player.playing} />
								{/if}
							</Cover>
							<div class="min-w-0 flex-1">
								<div class="flex min-w-0 items-center gap-1.5">
									{#if targetExplicit(target)}
										<ExplicitBadge />
									{/if}
									<span
										class="truncate text-sm text-fg transition-colors group-hover/row:text-accent-soft"
									>
										{targetTitle(target)}
									</span>
								</div>
								{#if targetArtist(target)}
									<div class="truncate text-xs text-fg-2">{targetArtist(target)}</div>
								{/if}
							</div>
							{#if targetDurationSeconds(target)}
								<span class="shrink-0 text-sm text-fg-2 tabular-nums">
									{fmtTime(targetDurationSeconds(target) ?? 0)}
								</span>
							{/if}
						</button>
					</li>
				{/each}
			</ul>
		</section>
	{/if}
</div>

{#if contextMenu}
	<TrackContextMenu
		menu={contextMenu}
		{playlists}
		onClose={() => (contextMenu = null)}
		onAddToQueue={addToQueue}
	/>
{/if}

{#if albumMenu}
	<AlbumContextMenu
		menu={albumMenu}
		{playlists}
		onClose={() => (albumMenu = null)}
		onPlayNext={albumPlayNext}
		onAddToQueue={albumAddToQueue}
	/>
{/if}
