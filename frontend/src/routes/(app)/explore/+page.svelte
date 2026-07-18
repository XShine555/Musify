<script lang="ts">
	import Search from '@lucide/svelte/icons/search';
	import Music from '@lucide/svelte/icons/music';
	import { goto } from '$app/navigation';
	import { player, queueIdForTrack, toQueueItems, type QueueItem } from '$lib/player/player.svelte';
	import Page from '$lib/components/ui/Page.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import MediaGrid from '$lib/components/ui/MediaGrid.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import InfiniteScroll from '$lib/components/ui/InfiniteScroll.svelte';
	import { EXPLORE_PAGE_SIZE } from '$lib/config';
	import { hueFor } from '$lib/theme/color';
	import type { YouTubeSong } from '$lib/types';
	import TrackTile from '$lib/components/TrackTile.svelte';
	import TrackContextMenu, {
		contextMenuStateFor,
		type ContextMenuTarget,
		type ContextMenuState
	} from '$lib/components/TrackContextMenu.svelte';
  import { appendUnique } from '$lib/collections.js';

	let { data, form } = $props();

	let searchValue = $state(data.query);
	let searchTimeout: ReturnType<typeof setTimeout> | undefined;

	let ytItems = $state<YouTubeSong[]>([]);
	let ytContinuation = $state('');
	let ytSearchTerm = $state('');
	let contextMenu = $state<ContextMenuState | null>(null);

	type LocalTrack = (typeof data.tracks.items)[number];

	let localItems = $state<LocalTrack[]>([]);
	let localPage = $state(1);
	let localHasNext = $state(false);
	let loadingMore = $state(false);

	$effect(() => {
		contextMenu = null;
		localItems = data.tracks.items;
		localPage = Number(data.tracks.pageNumber);
		localHasNext = Boolean(data.tracks.hasNextPage);

		if (data.query) {
			ytItems = appendUnique(
				[],
				(data.ytResults?.items ?? []) as YouTubeSong[],
				item => item.videoId
			);
			ytContinuation = data.ytResults?.continuationToken ?? '';
			ytSearchTerm = data.query;
			return;
		}
		ytItems = [];
		ytContinuation = '';
		ytSearchTerm = '';
		data.youtubeFiller?.then((filler) => {
			if (!filler) return;
			ytItems = appendUnique(
				[],
				filler.items,
				item => item.videoId
			);
			ytContinuation = filler.continuationToken;
			ytSearchTerm = filler.query;
		});
	});

	const items = $derived<ContextMenuTarget[]>([
		...localItems.map((track) => ({ kind: 'local' as const, track })),
		...ytItems.map((song) => ({ kind: 'youtube' as const, song }))
	]);

	const hasMore = $derived(localHasNext || ytContinuation !== '');

	const nothingFound = $derived(
		!!data.query && items.length === 0 && !data.needsAuth && !data.ytError
	);

	function itemId(item: ContextMenuTarget) {
		return item.kind === 'youtube' ? item.song.videoId : item.track.id;
	}

	function itemTitle(item: ContextMenuTarget) {
		return item.kind === 'youtube' ? item.song.title : item.track.title;
	}

	function itemArtist(item: ContextMenuTarget) {
		return item.kind === 'youtube' ? item.song.artist : item.track.artist;
	}

	function itemExplicit(item: ContextMenuTarget) {
		return item.kind === 'youtube' ? item.song.isExplicit : (item.track.isExplicit ?? false);
	}

	function itemActive(item: ContextMenuTarget) {
		return (
			player.current?.id === (item.kind === 'youtube' ? item.song.videoId : queueIdForTrack(item.track))
		);
	}

	function queueItemFor(target: ContextMenuTarget): QueueItem {
		return target.kind === 'youtube'
			? {
					id: target.song.videoId,
					title: target.song.title,
					artist: target.song.artist,
					source: 'youtube',
					coverUrl: target.song.thumbnailUrl,
					explicit: target.song.isExplicit
				}
			: toQueueItems([target.track])[0];
	}

	function buildHref(query: string) {
		return query ? `/explore?q=${encodeURIComponent(query)}` : '/explore';
	}

	function onSearchInput(event: Event) {
		const value = (event.currentTarget as HTMLInputElement).value;
		clearTimeout(searchTimeout);
		searchTimeout = setTimeout(() => {
			goto(buildHref(value.trim()), {
				keepFocus: true,
				replaceState: true,
				noScroll: true
			});
		}, 350);
	}

	function togglePlay(index: number) {
		player.playOrToggle(items.map(queueItemFor), index);
	}

	function openContextMenu(event: MouseEvent, target: ContextMenuTarget) {
		contextMenu = contextMenuStateFor(event, target);
	}

	function closeContextMenu() {
		contextMenu = null;
	}

	function addToQueue() {
		if (!contextMenu) return;
		player.addToQueue(queueItemFor(contextMenu));
		closeContextMenu();
	}

	async function loadMoreLocal() {
		const params = new URLSearchParams({
			pageNumber: String(localPage + 1),
			pageSize: String(EXPLORE_PAGE_SIZE)
		});
		if (data.query) params.set('name', data.query);
		const res = await fetch(`/api/tracks?${params}`);
		if (!res.ok) throw new Error(String(res.status));
		const next = (await res.json()) as typeof data.tracks;
		localItems = appendUnique(
			localItems,
			next.items,
			track => track.id
		);
		localPage = Number(next.pageNumber);
		localHasNext = Boolean(next.hasNextPage);
	}

	async function loadMoreYouTube() {
		const params = new URLSearchParams({ query: ytSearchTerm, continuation: ytContinuation });
		const res = await fetch(`/api/youtube/search?${params}`);
		if (!res.ok) throw new Error(String(res.status));
		const next = (await res.json()) as { items: YouTubeSong[]; continuationToken: string };
		ytItems = appendUnique(
			ytItems,
			next.items,
			item => item.videoId
		);
		ytContinuation = next.continuationToken;
	}

	async function loadMore() {
		if (loadingMore) return;
		loadingMore = true;
		try {
			if (localHasNext) await loadMoreLocal();
			else if (ytContinuation && ytSearchTerm) await loadMoreYouTube();
		} catch {
			if (localHasNext) localHasNext = false;
			else ytContinuation = '';
		} finally {
			loadingMore = false;
		}
	}
</script>

<svelte:head>
	<title>Explorar · Musify</title>
	<meta name="description" content="Explora tu música y busca canciones en YouTube Music." />
</svelte:head>

<Page>
	<PageHeader title="Explorar" subtitle="Busca a la vez en tu música y en YouTube Music." />

	<div class="mt-8">
		<Input
			type="search"
			icon={Search}
			bind:value={searchValue}
			oninput={onSearchInput}
			placeholder="Buscar en tu música y en YouTube Music..."
		/>
	</div>

	{#if form?.message}
		<Alert class="mt-4">{form.message}</Alert>
	{/if}

	{#if nothingFound}
		<EmptyState icon={Music} title="Sin resultados" description="No hay resultados para «{data.query}»." />
	{/if}

	{#if items.length > 0}
		<MediaGrid as="ul" class="mt-6">
			{#each items as item, i (itemId(item))}
				<TrackTile
					id={itemId(item)}
					title={itemTitle(item)}
					artist={itemArtist(item)}
					coverSrc={item.kind === 'youtube' ? item.song.thumbnailUrl : undefined}
					hue={hueFor(itemId(item))}
					explicit={itemExplicit(item)}
					active={itemActive(item)}
					playing={player.isPlaying}
					index={i}
					onClick={() => togglePlay(i)}
					onContextMenu={(e) => openContextMenu(e, item)}
				/>
			{/each}
		</MediaGrid>

		<InfiniteScroll onLoadMore={loadMore} {hasMore} loading={loadingMore} />
	{/if}

	{#if data.needsAuth}
		<EmptyState
			icon={Music}
			description="Inicia sesión para ver también resultados de YouTube Music."
		/>
	{:else if data.ytError}
		<EmptyState
			icon={Music}
			description="YouTube Music no está disponible ahora mismo. Inténtalo de nuevo."
		/>
	{/if}
</Page>

{#if contextMenu}
	<TrackContextMenu menu={contextMenu} playlists={data.playlists} onClose={closeContextMenu} onAddToQueue={addToQueue} />
{/if}
