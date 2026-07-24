<script lang="ts">
	import ArrowRight from '@lucide/svelte/icons/arrow-right';
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
	import Cover from '$lib/components/ui/Cover.svelte';
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
	import MixBentoTile from '$lib/components/ui/MixBentoTile.svelte';
	import Rail from '$lib/components/ui/Rail.svelte';
	import CoverBadge from '$lib/components/ui/CoverBadge.svelte';
	import NowPlaying from '$lib/components/ui/NowPlaying.svelte';
	import ExplicitBadge from '$lib/components/ui/ExplicitBadge.svelte';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';
	import TrackContextMenu, {
		contextMenuStateFor,
		type ContextMenuState
	} from '$lib/components/TrackContextMenu.svelte';
	import type { YouTubeSong } from '$lib/types';
	import { appendUnique } from '$lib/collections';
	import { HOME_LATEST_PAGE_SIZE } from '$lib/config';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import Music from '@lucide/svelte/icons/music';

	let { data } = $props();

	let contextMenu = $state<ContextMenuState | null>(null);
	let youtubeFillerItems = $state<YouTubeSong[]>([]);

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

	const albums = $derived(data.albums);
	const mixes = $derived(data.mixes);
	const playlists = $derived(data.playlists);

	const newTargets = $derived<TrackTarget[]>(
		data.newReleases.map((track) => ({ kind: 'local' as const, track }))
	);
	const recTargets = $derived<TrackTarget[]>(
		youtubeFillerItems.map((song) => ({ kind: 'youtube' as const, song }))
	);

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

	function playRecent(index: number) {
		player.playOrToggle(recentlyPlayed, index);
	}
	function playNew(index: number) {
		player.playOrToggle(newTargets.map(queueItemForTarget), index);
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

	function albumCaption(album: (typeof albums)[number]) {
		return [
			album.releaseYear ? String(album.releaseYear) : undefined,
			`${Number(album.trackCount)} ${Number(album.trackCount) === 1 ? 'canción' : 'canciones'}`
		]
			.filter(Boolean)
			.join(' · ');
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
		<h1 class="text-3xl leading-[1.05] font-semibold text-fg sm:text-6xl">
			Tu música. Sin límites.
		</h1>
	</div>
</section>

<div class="flex flex-col gap-10 page-x py-4 sm:py-6">
	{#if recentlyPlayed.length > 0}
		<section>
			<SectionHeading title="Recientemente reproducidas" />
			<div
				class="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5"
			>
				{#each recentlyPlayed as track, i (track.id)}
					<button
						type="button"
						onclick={() => playRecent(i)}
						oncontextmenu={(e) => openContextMenu(e, targetForQueueItem(track))}
						aria-label="{player.current.id === track.id && player.playing
							? 'Pausar'
							: 'Reproducir'} {track.title}"
						class="group/row flex min-w-0 items-center gap-3 rounded-control bg-surface p-2.5 text-left transition duration-200"
					>
						<Cover
							trackId={track.id}
							src={track.coverUrl}
							size="small"
							alt={track.title}
							class="h-14 w-14 shrink-0 rounded-control shadow-art transition duration-200"
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
								<span class="truncate text-fg transition-colors group-hover/row:text-accent-soft">
									{track.title}
								</span>
							</div>
							{#if track.artist}
								<div class="truncate text-sm text-muted">{track.artist}</div>
							{/if}
						</div>
					</button>
				{/each}
			</div>
		</section>
	{/if}

	{#if albums.length > 0}
		<section>
			<SectionHeading title="Álbumes recientes" />
			<Rail>
				{#each albums as album, i (album.id)}
					<a
						href={album.youTubeAlbumId
							? `/albums/youtube/${album.youTubeAlbumId}`
							: `/albums/${album.id}`}
						class="group/card animate-enter block w-42 shrink-0 rounded-art focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg focus-visible:outline-none"
						style="animation-delay:{Math.min(i, 10) * 45}ms"
					>
						<div class="relative">
							{#if album.thumbnailUrl}
								<Cover
									trackId={album.id}
									src={album.thumbnailUrl}
									size="large"
									alt={album.title}
									class="h-52 w-42 rounded-art shadow-art ring-line transition duration-300 ease-out"
								/>
							{:else}
								<PlaylistArt
									trackIds={album.coverTrackIds}
									class="h-56 w-48 rounded-art shadow-art ring-line transition duration-300 ease-out"
								/>
							{/if}
						</div>
						<div class="mt-2.5 text-fg transition-colors group-hover/card:text-accent-soft">
							{album.title}
						</div>
						<div class="mt-0.5 text-sm text-fg-3">{albumCaption(album)}</div>
					</a>
				{/each}
			</Rail>
		</section>
	{/if}

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
						class="group/card animate-enter block w-38 shrink-0 rounded-art"
						style="animation-delay:{Math.min(i, 10) * 45}ms"
					>
						<PlaylistArt
							playlistId={playlist.id}
							trackIds={playlist.coverTrackIds}
							version={playlist.updatedAt}
							class="h-38 w-38 rounded-art"
						/>
						<div
							class="mt-2.5 truncate text-fg transition-colors group-hover/card:text-accent-soft"
						>
							{playlist.name}
						</div>
						{#if playlist.description}
							<div class="mt-0.5 truncate text-sm text-muted">{playlist.description}</div>
						{/if}
					</a>
				{/each}
				<a
					href="/playlists"
					class="grid h-36 w-36 shrink-0 place-items-center text-fg-3 transition-colors hover:text-accent-soft"
				>
					<div class="flex flex-col items-center gap-2">
						<span class="grid h-8 w-8 place-items-center">
							<ArrowRight class="h-4 w-4" />
						</span>
						Ver todas
					</div>
				</a>
			</Rail>
		</section>
	{/if}

	{#if newTargets.length > 0}
		<section>
			<SectionHeading title="Canciones nuevas" />
			<Rail>
				{#each newTargets as target, i (targetId(target))}
					<button
						type="button"
						onclick={() => playNew(i)}
						oncontextmenu={(e) => openContextMenu(e, target)}
						aria-label="Reproducir {targetTitle(target)}"
						class="group/card animate-enter block w-[148px] shrink-0 rounded-art text-left focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg focus-visible:outline-none"
						style="animation-delay:{Math.min(i, 10) * 45}ms"
					>
						<div class="relative h-[198px] w-[148px]">
							<Cover
								trackId={targetId(target)}
								src={targetCoverSrc(target)}
								size="medium"
								alt={targetTitle(target)}
								class="h-full w-full rounded-art shadow-art ring-1 ring-line transition duration-300 ease-out ring-inset group-hover/card:shadow-art-lg"
							>
								{#if isTargetCurrent(target)}
									<NowPlaying paused={!player.playing} />
								{/if}
							</Cover>
							<CoverBadge label="Nuevo" />
						</div>
						<div class="mt-2.5 flex min-w-0 items-center gap-1.5">
							{#if targetExplicit(target)}
								<ExplicitBadge />
							{/if}
							<span class="truncate text-fg transition-colors group-hover/card:text-accent-soft">
								{targetTitle(target)}
							</span>
						</div>
						{#if targetArtist(target)}
							<div class="mt-0.5 truncate text-sm text-fg-3">{targetArtist(target)}</div>
						{/if}
					</button>
				{/each}
			</Rail>
		</section>
	{/if}
	{#if recentlyPlayed.length === 0 && albums.length === 0 && mixes.length === 0 && playlists.length === 0 && newTargets.length === 0}
		<EmptyState icon={Music} title="Todavía no hay música" description="Sube tu música o explora nuevas canciones, álbumes y listas de reproducción." />
	{/if}
</div>

<!---
<section class="page-x pt-10 pb-16">
	{#if recTargets.length > 0}
		<Rail>
			{#each recTargets as target, i (targetId(target))}
				<button
					type="button"
					onclick={() => playRecommended(i)}
					oncontextmenu={(e) => openContextMenu(e, target)}
					aria-label="Reproducir {targetTitle(target)}"
					class="group/card animate-enter block w-[168px] shrink-0 rounded-art text-left focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg focus-visible:outline-none"
					style="animation-delay:{Math.min(i, 10) * 45}ms"
				>
					<div class="relative h-[168px] w-[168px]">
						<Cover
							trackId={targetId(target)}
							src={targetCoverSrc(target)}
							size="medium"
							alt={targetTitle(target)}
							class="h-full w-full rounded-art shadow-art ring-1 ring-line transition duration-300 ease-out ring-inset group-hover/card:shadow-art-lg"
						>
							{#if isTargetCurrent(target)}
								<NowPlaying paused={!player.playing} />
							{/if}
						</Cover>
					</div>
					<div class="mt-2.5 flex min-w-0 items-center gap-1.5">
						{#if targetExplicit(target)}
							<ExplicitBadge />
						{/if}
						<span class="truncate text-fg transition-colors group-hover/card:text-accent-soft">
							{targetTitle(target)}
						</span>
					</div>
					{#if targetArtist(target)}
						<div class="mt-0.5 truncate text-sm text-fg-3">{targetArtist(target)}</div>
					{/if}
				</button>
			{/each}
		</Rail>
	{:else}
		<EmptyState icon={Music} title="Todavía no hay recomendaciones" />
	{/if}
</section>-->

{#if contextMenu}
	<TrackContextMenu
		menu={contextMenu}
		{playlists}
		onClose={() => (contextMenu = null)}
		onAddToQueue={addToQueue}
	/>
{/if}
