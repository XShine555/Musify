<script lang="ts">
	import Search from '@lucide/svelte/icons/search';
	import Music from '@lucide/svelte/icons/music';
	import { untrack } from 'svelte';
	import { goto } from '$app/navigation';
	import { player } from '$lib/player/player.svelte';
	import { fetchAlbumQueueItems } from '$lib/albums';
	import { fmtTime } from '$lib/format';
	import Page from '$lib/components/ui/Page.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import MediaGrid from '$lib/components/ui/MediaGrid.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import InfiniteScroll from '$lib/components/ui/InfiniteScroll.svelte';
	import Cover from '$lib/components/ui/Cover.svelte';
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
	import ArtistAvatar from '$lib/components/ui/ArtistAvatar.svelte';
	import Rail from '$lib/components/ui/Rail.svelte';
	import NowPlaying from '$lib/components/ui/NowPlaying.svelte';
	import ExplicitBadge from '$lib/components/ui/ExplicitBadge.svelte';
	import SectionHeading from '$lib/components/ui/SectionHeading.svelte';
	import { EXPLORE_ALBUMS_LIMIT, EXPLORE_PAGE_SIZE, SEARCH_DEBOUNCE_MS } from '$lib/config';
	import type { YouTubeSong } from '$lib/types';
	import TrackContextMenu, {
		contextMenuStateFor,
		type ContextMenuState
	} from '$lib/components/TrackContextMenu.svelte';
	import AlbumContextMenu, {
		albumContextMenuStateFor,
		type AlbumMenuState
	} from '$lib/components/AlbumContextMenu.svelte';
	import { appendUnique } from '$lib/collections';
	import {
		isTargetCurrent,
		queueItemForTarget,
		targetArtist,
		targetCoverSrc,
		targetExplicit,
		targetId,
		targetTitle,
		type TrackTarget
	} from '$lib/tracks';

	let { data, form } = $props();

	let searchValue = $state(untrack(() => data.query));
	let searchTimeout: ReturnType<typeof setTimeout> | undefined;

	let ytItems = $state<YouTubeSong[]>([]);
	let ytUnavailable = $state(false);
	let contextMenu = $state<ContextMenuState | null>(null);
	let albumMenu = $state<AlbumMenuState | null>(null);

	type LocalTrack = NonNullable<(typeof data.tracks.items)[number]['track']>;

	let localItems = $state<LocalTrack[]>([]);
	let localPage = $state(1);
	let ytContinuation = $state('');
	let hasMore = $state(false);
	let loadingMore = $state(false);

	$effect(() => {
		contextMenu = null;
		albumMenu = null;
		localItems = data.tracks.items.flatMap((item) => (item.track ? [item.track] : []));
		localPage = Number(data.tracks.pageNumber);
		ytContinuation = data.tracks.nextYoutubeContinuationToken ?? '';
		hasMore = data.tracks.hasNextPage;

		if (data.query) {
			ytItems = appendUnique(
				[],
				data.tracks.items.flatMap((item) => (item.youTubeSong ? [item.youTubeSong as YouTubeSong] : [])),
				(i) => i.videoId
			);
			ytUnavailable = data.tracks.youtubeUnavailable;
			return;
		}

		ytItems = [];
		ytUnavailable = false;

		let cancelled = false;
		data.youtubeFiller?.then((filler) => {
			if (cancelled || !filler) return;
			ytItems = appendUnique([], filler.items, (i) => i.videoId);
		});
		return () => {
			cancelled = true;
		};
	});

	const songRows = $derived([
		...localItems.map((track) => ({
			target: { kind: 'local' as const, track },
			seconds: Number(track.duration)
		})),
		...ytItems.map((song) => ({
			target: { kind: 'youtube' as const, song },
			seconds: Number(song.durationSeconds)
		}))
	] satisfies { target: TrackTarget; seconds: number }[]);

	const items = $derived<TrackTarget[]>(songRows.map((row) => row.target));

	const albums = $derived(data.albums);
	const youtubeAlbums = $derived(data.youtubeAlbums);
	const users = $derived(data.users);

	type LocalAlbum = (typeof data.albums)[number];
	type YouTubeAlbum = (typeof data.youtubeAlbums)[number];
	type AlbumEntry = { kind: 'local'; album: LocalAlbum } | { kind: 'youtube'; album: YouTubeAlbum };

	const albumEntries = $derived<AlbumEntry[]>(
		[
			...albums.map((album) => ({ kind: 'local' as const, album })),
			...youtubeAlbums.map((album) => ({ kind: 'youtube' as const, album }))
		].slice(0, EXPLORE_ALBUMS_LIMIT)
	);

	const hasAlbums = $derived(albumEntries.length > 0);
	const hasUsers = $derived(users.length > 0);

	const nothingFound = $derived(
		!!data.query && !ytUnavailable && items.length === 0 && !hasAlbums && !hasUsers
	);

	function localAlbumCaption(album: LocalAlbum) {
		return [
			album.releaseYear === null ? undefined : String(album.releaseYear),
			`${Number(album.trackCount)} ${Number(album.trackCount) === 1 ? 'canción' : 'canciones'}`
		]
			.filter(Boolean)
			.join(' · ');
	}

	function youtubeAlbumCaption(album: YouTubeAlbum) {
		return [album.artist, album.releaseYear === null ? undefined : String(album.releaseYear)]
			.filter(Boolean)
			.join(' · ');
	}

	function buildHref(query: string) {
		return query ? `/explore?q=${encodeURIComponent(query)}` : '/explore';
	}

	function onSearchInput(event: Event) {
		const value = (event.currentTarget as HTMLInputElement).value;
		clearTimeout(searchTimeout);
		searchTimeout = setTimeout(() => {
			const term = value.trim();
			if (term === data.query) return;
			goto(buildHref(term), { keepFocus: true, replaceState: true, noScroll: true });
		}, SEARCH_DEBOUNCE_MS);
	}

	function togglePlay(index: number) {
		player.playOrToggle(items.map(queueItemForTarget), index);
	}

	function openContextMenu(event: MouseEvent, target: TrackTarget) {
		contextMenu = contextMenuStateFor(event, target);
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

	function addToQueue() {
		if (!contextMenu) return;
		player.addToQueue(queueItemForTarget(contextMenu));
		contextMenu = null;
	}

	async function loadMore() {
		if (loadingMore) return;
		loadingMore = true;
		try {
			const params = new URLSearchParams({
				pageNumber: String(localPage + 1),
				pageSize: String(EXPLORE_PAGE_SIZE)
			});
			if (data.query) {
				params.set('name', data.query);
				if (ytContinuation) params.set('youtubeContinuationToken', ytContinuation);
			}
			const res = await fetch(`/api/tracks?${params}`);
			if (!res.ok) throw new Error(String(res.status));
			const next = (await res.json()) as typeof data.tracks;
			localItems = appendUnique(
				localItems,
				next.items.flatMap((item) => (item.track ? [item.track] : [])),
				(track) => track.id
			);
			ytItems = appendUnique(
				ytItems,
				next.items.flatMap((item) => (item.youTubeSong ? [item.youTubeSong as YouTubeSong] : [])),
				(song) => song.videoId
			);
			localPage = Number(next.pageNumber);
			ytContinuation = next.nextYoutubeContinuationToken ?? '';
			hasMore = next.hasNextPage;
		} catch {
			hasMore = false;
		} finally {
			loadingMore = false;
		}
	}
</script>

<svelte:head>
	<title>Buscar</title>
	<meta name="description" content="Busca canciones, álbumes y usuarios en Musify." />
</svelte:head>

<Page>
	<PageHeader title="Buscar" subtitle="Encuentra canciones, álbumes y personas." />

	<div class="mt-6">
		<Input
			type="search"
			icon={Search}
			bind:value={searchValue}
			oninput={onSearchInput}
			placeholder="¿Qué quieres reproducir?"
		/>
	</div>

	{#if form?.message}
		<Alert class="mt-4">{form.message}</Alert>
	{/if}

	{#if nothingFound}
		<EmptyState
			icon={Music}
			title="Sin resultados"
			description="No hay resultados para «{data.query}»."
		/>
	{/if}

	{#if hasAlbums}
		<div class="mt-8">
			<MediaGrid as="ul" min="168px" minMobile="150px">
				{#each albumEntries as entry, i (entry.kind === 'local' ? entry.album.id : entry.album.albumId)}
					<li>
						{#if entry.kind === 'local'}
							<a
								href="/albums/{entry.album.id}"
								class="group/card animate-enter block min-w-0 rounded-art focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg focus-visible:outline-none"
								style="animation-delay:{Math.min(i, 10) * 45}ms"
								oncontextmenu={(e) => openAlbumMenu(e, 'local', entry.album.id)}
							>
								<div class="relative">
									<PlaylistArt
										trackIds={entry.album.coverTrackIds}
										class="aspect-square w-full rounded-art shadow-art ring-1 ring-line transition duration-300 ease-out ring-inset group-hover/card:shadow-art-lg"
									/>
								</div>
								<div
									class="mt-2.5 truncate text-fg transition-colors group-hover/card:text-accent-soft"
								>
									{entry.album.title}
								</div>
								<div class="mt-0.5 truncate text-sm text-fg-3">
									{localAlbumCaption(entry.album)}
								</div>
							</a>
						{:else}
							<a
								href="/albums/youtube/{entry.album.albumId}"
								class="group/card animate-enter block min-w-0 rounded-art focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg focus-visible:outline-none"
								style="animation-delay:{Math.min(i, 10) * 45}ms"
								oncontextmenu={(e) => openAlbumMenu(e, 'youtube', entry.album.albumId)}
							>
								<div class="relative">
									<Cover
										trackId={entry.album.albumId}
										src={entry.album.thumbnailUrl}
										size="large"
										alt={entry.album.title}
										class="aspect-square w-full rounded-art shadow-art ring-1 ring-line transition duration-300 ease-out ring-inset group-hover/card:shadow-art-lg"
									/>
								</div>
								<div
									class="mt-2.5 truncate text-fg transition-colors group-hover/card:text-accent-soft"
								>
									{entry.album.title}
								</div>
								<div class="mt-0.5 truncate text-sm text-fg-3">
									{youtubeAlbumCaption(entry.album)}
								</div>
							</a>
						{/if}
					</li>
				{/each}
			</MediaGrid>
		</div>
	{/if}

	{#if hasUsers}
		<div class="mt-8">
			<SectionHeading title="Usuarios" />
			<Rail>
				{#each users as u, i (u.id)}
					<div
						class="group/artist animate-enter shrink-0"
						style="animation-delay:{Math.min(i, 10) * 45}ms"
					>
						<ArtistAvatar name={u.name} size={96} />
						<div class="mt-1 w-24 truncate text-center text-xs text-fg-3">@{u.handle}</div>
					</div>
				{/each}
			</Rail>
		</div>
	{/if}

	<div class="mt-6">
		{#if songRows.length > 0}
			<ul class="flex flex-col gap-1">
				{#each songRows as { target: item, seconds }, i (targetId(item))}
					<li>
						<button
							type="button"
							onclick={() => togglePlay(i)}
							oncontextmenu={(e) => openContextMenu(e, item)}
							aria-label="Reproducir {targetTitle(item)}"
							class="group/row flex w-full min-w-0 items-center gap-3.5 rounded-control p-2 pr-4 text-left transition hover:bg-surface focus-visible:ring-2 focus-visible:ring-accent focus-visible:outline-none"
						>
							<Cover
								trackId={targetId(item)}
								src={targetCoverSrc(item)}
								size="small"
								alt={targetTitle(item)}
								class="h-11 w-11 shrink-0 rounded-control shadow-art ring-1 ring-line ring-inset"
							>
								{#if isTargetCurrent(item)}
									<NowPlaying paused={!player.playing} />
								{/if}
							</Cover>
							<div class="min-w-0 flex-1">
								<div class="flex min-w-0 items-center gap-1.5">
									{#if targetExplicit(item)}
										<ExplicitBadge />
									{/if}
									<span class="truncate text-fg transition-colors group-hover/row:text-accent-soft">
										{targetTitle(item)}
									</span>
								</div>
								{#if targetArtist(item)}
									<div class="truncate text-sm text-fg-3">Canción · {targetArtist(item)}</div>
								{/if}
							</div>
							<span class="shrink-0 text-sm text-muted tabular-nums">{fmtTime(seconds)}</span>
						</button>
					</li>
				{/each}
			</ul>

			{#if hasMore}
				<InfiniteScroll onLoadMore={loadMore} {hasMore} loading={loadingMore} />
			{/if}
		{:else if data.query && !ytUnavailable}
			<EmptyState icon={Music} description="No hay canciones que coincidan con «{data.query}»." />
		{/if}
	</div>

	{#if ytUnavailable}
		<EmptyState
			icon={Music}
			description="YouTube Music no está disponible ahora mismo. Inténtalo de nuevo."
		/>
	{/if}
</Page>

{#if contextMenu}
	<TrackContextMenu
		menu={contextMenu}
		playlists={data.playlists}
		onClose={() => (contextMenu = null)}
		onAddToQueue={addToQueue}
	/>
{/if}

{#if albumMenu}
	<AlbumContextMenu
		menu={albumMenu}
		playlists={data.playlists}
		onClose={() => (albumMenu = null)}
		onPlayNext={albumPlayNext}
		onAddToQueue={albumAddToQueue}
	/>
{/if}
