<script lang="ts">
	import Search from '@lucide/svelte/icons/search';
	import Music from '@lucide/svelte/icons/music';
	import { untrack } from 'svelte';
	import { goto } from '$app/navigation';
	import { player } from '$lib/player/player.svelte';
	import Page from '$lib/components/ui/Page.svelte';
	import PageHeader from '$lib/components/ui/PageHeader.svelte';
	import MediaGrid from '$lib/components/ui/MediaGrid.svelte';
	import Alert from '$lib/components/ui/Alert.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import InfiniteScroll from '$lib/components/ui/InfiniteScroll.svelte';
	import { EXPLORE_PAGE_SIZE, SEARCH_DEBOUNCE_MS } from '$lib/config';
	import { hueFor } from '$lib/theme/color';
	import type { YouTubeSong } from '$lib/types';
	import TrackTile from '$lib/components/TrackTile.svelte';
	import TrackContextMenu, {
		contextMenuStateFor,
		type ContextMenuState
	} from '$lib/components/TrackContextMenu.svelte';
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
			ytItems = appendUnique([], (data.ytResults?.items ?? []) as YouTubeSong[], (i) => i.videoId);
			ytContinuation = data.ytResults?.continuationToken ?? '';
			ytSearchTerm = data.query;
			return;
		}

		ytItems = [];
		ytContinuation = '';
		ytSearchTerm = '';

		let cancelled = false;
		data.youtubeFiller?.then((filler) => {
			if (cancelled || !filler) return;
			ytItems = appendUnique([], filler.items, (i) => i.videoId);
			ytContinuation = filler.continuationToken;
			ytSearchTerm = filler.query;
		});
		return () => {
			cancelled = true;
		};
	});

	const items = $derived<TrackTarget[]>([
		...localItems.map((track) => ({ kind: 'local' as const, track })),
		...ytItems.map((song) => ({ kind: 'youtube' as const, song }))
	]);

	const hasMore = $derived(localHasNext || ytContinuation !== '');

	const nothingFound = $derived(!!data.query && items.length === 0 && !data.ytError);

	function buildHref(query: string) {
		return query ? `/explore?q=${encodeURIComponent(query)}` : '/explore';
	}

	function onSearchInput(event: Event) {
		const value = (event.currentTarget as HTMLInputElement).value;
		clearTimeout(searchTimeout);
		searchTimeout = setTimeout(() => {
			const term = value.trim();
			if (term === data.query) return;
			goto(buildHref(term), {
				keepFocus: true,
				replaceState: true,
				noScroll: true
			});
		}, SEARCH_DEBOUNCE_MS);
	}

	function togglePlay(index: number) {
		player.playOrToggle(items.map(queueItemForTarget), index);
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

	async function loadMoreLocal() {
		const params = new URLSearchParams({
			pageNumber: String(localPage + 1),
			pageSize: String(EXPLORE_PAGE_SIZE)
		});
		if (data.query) params.set('name', data.query);
		const res = await fetch(`/api/tracks?${params}`);
		if (!res.ok) throw new Error(String(res.status));
		const next = (await res.json()) as typeof data.tracks;
		localItems = appendUnique(localItems, next.items, (track) => track.id);
		localPage = Number(next.pageNumber);
		localHasNext = Boolean(next.hasNextPage);
	}

	async function loadMoreYouTube() {
		const params = new URLSearchParams({ query: ytSearchTerm, continuation: ytContinuation });
		const res = await fetch(`/api/youtube/search?${params}`);
		if (!res.ok) throw new Error(String(res.status));
		const next = (await res.json()) as { items: YouTubeSong[]; continuationToken: string };
		ytItems = appendUnique(ytItems, next.items, (i) => i.videoId);
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
		<EmptyState
			icon={Music}
			title="Sin resultados"
			description="No hay resultados para «{data.query}»."
		/>
	{/if}

	{#if items.length > 0}
		<MediaGrid as="ul" class="mt-6">
			{#each items as item, i (targetId(item))}
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
					onClick={() => togglePlay(i)}
					onContextMenu={(e) => openContextMenu(e, item)}
				/>
			{/each}
		</MediaGrid>

		<InfiniteScroll onLoadMore={loadMore} {hasMore} loading={loadingMore} />
	{/if}

	{#if data.ytError}
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
		onClose={closeContextMenu}
		onAddToQueue={addToQueue}
	/>
{/if}
