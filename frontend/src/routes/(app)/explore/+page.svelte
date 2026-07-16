<script lang="ts">
	import Search from '@lucide/svelte/icons/search';
	import Music from '@lucide/svelte/icons/music';
	import ChevronRight from '@lucide/svelte/icons/chevron-right';
	import ListPlus from '@lucide/svelte/icons/list-plus';
	import LoaderCircle from '@lucide/svelte/icons/loader-circle';
	import { goto } from '$app/navigation';
	import { enhance } from '$app/forms';
	import {
		player,
		queueIdForTrack,
		toQueueItems,
		type ApiTrackLike,
		type QueueItem
	} from '$lib/player/player.svelte';
	import Cover from '$lib/components/ui/Cover.svelte';
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
	import { HUES } from '$lib/theme/color';

	interface YouTubeSong {
		videoId: string;
		title: string;
		artist: string;
		album: string;
		durationSeconds: number;
		thumbnailUrl: string;
		isExplicit: boolean;
	}

	type ContextMenuTarget =
		| { kind: 'youtube'; song: YouTubeSong }
		| { kind: 'local'; track: ApiTrackLike };

	let { data, form } = $props();

	let searchValue = $state(data.query);
	let searchTimeout: ReturnType<typeof setTimeout> | undefined;

	let ytItems = $state<YouTubeSong[]>([]);
	let ytContinuation = $state('');
	let ytLoadingMore = $state(false);
	let contextMenu = $state<(ContextMenuTarget & { x: number; y: number; openLeft: boolean }) | null>(
		null
	);

	$effect(() => {
		ytItems = (data.ytResults?.items ?? []) as YouTubeSong[];
		ytContinuation = data.ytResults?.continuationToken ?? '';
		contextMenu = null;
	});

	const tracks = $derived(data.tracks);
	const currentPage = $derived(Number(data.tracks.pageNumber));
	const total = $derived(Number(data.tracks.totalItemCount));

	const localVisible = $derived(!data.query || tracks.items.length > 0);
	const youtubeVisible = $derived(
		!!data.query && (data.needsAuth || data.ytError || ytItems.length > 0)
	);
	const localHeadingVisible = $derived(!!data.query && localVisible);
	const youtubeHeadingVisible = $derived(!!data.query && youtubeVisible);
	const nothingFound = $derived(
		!!data.query && !localVisible && !youtubeVisible && !data.needsAuth && !data.ytError
	);

	function buildHref(page = 1, query = data.query) {
		const params = new URLSearchParams();
		if (query) params.set('q', query);
		if (page > 1) params.set('page', String(page));
		const qs = params.toString();
		return qs ? `/explore?${qs}` : '/explore';
	}

	function onSearchInput(event: Event) {
		const value = (event.currentTarget as HTMLInputElement).value;
		clearTimeout(searchTimeout);
		searchTimeout = setTimeout(() => {
			goto(buildHref(1, value.trim()), {
				keepFocus: true,
				replaceState: true,
				noScroll: true
			});
		}, 350);
	}

	function hueFor(id: string) {
		let hash = 0;
		for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) >>> 0;
		return HUES[hash % HUES.length];
	}

	function togglePlayLocal(index: number) {
		const track = data.tracks.items[index];
		if (player.current?.id === queueIdForTrack(track)) player.toggle();
		else player.playQueue(toQueueItems(data.tracks.items), index);
	}

	function togglePlayYouTube(index: number) {
		const song = ytItems[index];
		if (player.current?.id === song.videoId) {
			player.toggle();
			return;
		}
		player.playQueue(
			ytItems.map((s) => ({
				id: s.videoId,
				title: s.title,
				artist: s.artist,
				source: 'youtube' as const,
				coverUrl: s.thumbnailUrl
			})),
			index
		);
	}

	function openContextMenu(event: MouseEvent, target: ContextMenuTarget) {
		if (data.playlists.length === 0) return;
		event.preventDefault();
		const menuWidth = 208;
		const submenuWidth = 224;
		contextMenu = {
			...target,
			x: Math.min(event.clientX, window.innerWidth - menuWidth - 8),
			y: Math.min(event.clientY, window.innerHeight - 60),
			openLeft: event.clientX + menuWidth + submenuWidth + 16 > window.innerWidth
		};
	}

	function closeContextMenu() {
		contextMenu = null;
	}

	function queueItemFor(target: ContextMenuTarget): QueueItem {
		return target.kind === 'youtube'
			? {
					id: target.song.videoId,
					title: target.song.title,
					artist: target.song.artist,
					source: 'youtube',
					coverUrl: target.song.thumbnailUrl
				}
			: toQueueItems([target.track])[0];
	}

	function addToQueue(target: ContextMenuTarget | null) {
		if (!target) return;
		player.addToQueue(queueItemFor(target));
		closeContextMenu();
	}

	async function loadMoreYouTube() {
		if (!ytContinuation || ytLoadingMore) return;
		ytLoadingMore = true;
		try {
			const params = new URLSearchParams({ query: data.query, continuation: ytContinuation });
			const res = await fetch(`/api/youtube/search?${params}`);
			if (!res.ok) throw new Error(String(res.status));
			const next = (await res.json()) as { items: YouTubeSong[]; continuationToken: string };
			ytItems = [...ytItems, ...next.items];
			ytContinuation = next.continuationToken;
		} catch {
			ytContinuation = '';
		} finally {
			ytLoadingMore = false;
		}
	}
</script>

<svelte:head>
	<title>Explorar · Musify</title>
	<meta name="description" content="Explora tu música y busca canciones en YouTube Music." />
</svelte:head>

<section class="px-8 pt-12 pb-8">
	<h1 class="font-display text-3xl font-bold tracking-tight sm:text-4xl">Explorar</h1>
	<p class="mt-2 text-neutral-400">Busca a la vez en tu música y en YouTube Music.</p>

	<div class="relative mt-8">
		<span class="pointer-events-none absolute inset-y-0 left-4 grid place-items-center text-neutral-500">
			<Search class="h-4 w-4" />
		</span>
		<input
			type="search"
			bind:value={searchValue}
			oninput={onSearchInput}
			placeholder="Buscar en tu música y en YouTube Music..."
			autocomplete="off"
			class="w-full rounded-lg border border-white/15 bg-neutral-950 py-3 pl-11 pr-4 text-sm text-neutral-100 placeholder:text-neutral-500 focus:border-[var(--mf-accent)]/50 focus:outline-none"
		/>
	</div>

	{#if form?.message}
		<p class="mt-4 rounded-xl border border-white/10 bg-neutral-900/40 px-4 py-3 text-sm text-neutral-300">
			{form.message}
		</p>
	{/if}

	{#if nothingFound}
		<div class="mt-10 rounded-2xl border border-white/10 bg-neutral-900/40 p-12 text-center backdrop-blur-md">
			<Music class="mx-auto h-10 w-10 text-neutral-500" />
			<p class="mt-4 text-neutral-300">No hay resultados para «{data.query}».</p>
		</div>
	{/if}

	{#if localVisible}
		{#if localHeadingVisible}
			<h2 class="mt-10 font-display text-lg font-bold tracking-tight">Musify</h2>
		{/if}

		<p class="{localHeadingVisible ? 'mt-1' : 'mt-6'} text-sm text-neutral-500">
			{#if data.query}
				{total}
				{total === 1 ? 'resultado' : 'resultados'} para «{data.query}»
			{:else}
				{total}
				{total === 1 ? 'canción' : 'canciones'}
			{/if}
		</p>

		{#if tracks.items.length === 0}
			<div class="mt-6 rounded-2xl border border-white/10 bg-neutral-900/40 p-10 text-center backdrop-blur-md">
				<Music class="mx-auto h-8 w-8 text-neutral-500" />
				<p class="mt-3 text-sm text-neutral-300">Todavía no hay canciones. ¡Sé el primero en subir una!</p>
			</div>
		{:else}
			<ul class="mt-6 grid grid-cols-[repeat(auto-fill,minmax(160px,1fr))] gap-4">
				{#each tracks.items as track, i (track.id)}
					<li
						class="group animate-enter relative"
						style="animation-delay:{i * 40}ms"
						oncontextmenu={(e) => openContextMenu(e, { kind: 'local', track })}
					>
						<button type="button" onclick={() => togglePlayLocal(i)} class="block w-full text-left">
							<Cover
								trackId={track.id}
								hue={hueFor(track.id)}
								size="large"
								alt={track.title}
								class="aspect-square w-full rounded-xl shadow-[0_12px_28px_-10px_rgba(0,0,0,0.6)]"
							>
								<span
									class="absolute right-2.5 bottom-2.5 grid h-11 w-11 translate-y-2 place-items-center rounded-full bg-[var(--mf-accent)] text-neutral-950 opacity-0 shadow-lg transition-all group-hover:translate-y-0 group-hover:opacity-100"
									class:!opacity-100={player.current?.id === queueIdForTrack(track)}
									class:!translate-y-0={player.current?.id === queueIdForTrack(track)}
								>
									{#if player.current?.id === queueIdForTrack(track) && player.isPlaying}
										<svg viewBox="0 0 24 24" fill="currentColor" class="h-5 w-5">
											<path d="M6 5h4v14H6zM14 5h4v14h-4z" />
										</svg>
									{:else}
										<svg viewBox="0 0 24 24" fill="currentColor" class="h-5 w-5">
											<path d="M8 5v14l11-7z" />
										</svg>
									{/if}
								</span>
							</Cover>
							<div class="mt-2.5 truncate text-sm font-semibold text-[var(--mf-text)]" title={track.title}>
								{track.title}
							</div>
							{#if track.artist}
								<div class="mt-0.5 truncate text-xs text-neutral-500">{track.artist}</div>
							{/if}
						</button>
					</li>
				{/each}
			</ul>

			{#if tracks.hasPreviousPage || tracks.hasNextPage}
				<nav class="mt-8 flex items-center justify-center gap-4">
					{#if tracks.hasPreviousPage}
						<a
							href={buildHref(currentPage - 1)}
							class="rounded-lg border border-white/10 px-5 py-2 text-sm text-white/65 transition hover:bg-white/2.5"
						>
							Anterior
						</a>
					{/if}
					<span class="text-sm text-neutral-500">Página {currentPage}</span>
					{#if tracks.hasNextPage}
						<a
							href={buildHref(currentPage + 1)}
							class="rounded-lg border border-white/10 px-5 py-2 text-sm text-white/65 transition hover:bg-white/2.5"
						>
							Siguiente
						</a>
					{/if}
				</nav>
			{/if}
		{/if}
	{/if}

	{#if youtubeVisible}
		{#if youtubeHeadingVisible}
			<h2 class="mt-12 font-display text-lg font-bold tracking-tight">YouTube Music</h2>
		{/if}

		{#if data.needsAuth}
			<div class="mt-6 rounded-2xl border border-white/10 bg-neutral-900/40 p-10 text-center backdrop-blur-md">
				<Music class="mx-auto h-8 w-8 text-neutral-500" />
				<p class="mt-3 text-sm text-neutral-300">Inicia sesión para buscar en YouTube Music.</p>
			</div>
		{:else if data.ytError}
			<div class="mt-6 rounded-2xl border border-white/10 bg-neutral-900/40 p-10 text-center backdrop-blur-md">
				<Music class="mx-auto h-8 w-8 text-neutral-500" />
				<p class="mt-3 text-sm text-neutral-300">YouTube Music no está disponible ahora mismo. Inténtalo de nuevo.</p>
			</div>
		{:else}
			<ul class="mt-6 grid grid-cols-[repeat(auto-fill,minmax(160px,1fr))] gap-4">
				{#each ytItems as song, i (song.videoId)}
					<li
						class="group animate-enter relative"
						style="animation-delay:{i * 40}ms"
						oncontextmenu={(e) => openContextMenu(e, { kind: 'youtube', song })}
					>
						<button type="button" onclick={() => togglePlayYouTube(i)} class="block w-full text-left">
							<Cover
								trackId={song.videoId}
								src={song.thumbnailUrl}
								hue={hueFor(song.videoId)}
								alt={song.title}
								class="aspect-square w-full rounded-xl shadow-[0_12px_28px_-10px_rgba(0,0,0,0.6)]"
							>
								<span
									class="absolute right-2.5 bottom-2.5 grid h-11 w-11 translate-y-2 place-items-center rounded-full bg-[var(--mf-accent)] text-neutral-950 opacity-0 shadow-lg transition-all group-hover:translate-y-0 group-hover:opacity-100"
									class:!opacity-100={player.current?.id === song.videoId}
									class:!translate-y-0={player.current?.id === song.videoId}
								>
									{#if player.current?.id === song.videoId && player.isPlaying}
										<svg viewBox="0 0 24 24" fill="currentColor" class="h-5 w-5">
											<path d="M6 5h4v14H6zM14 5h4v14h-4z" />
										</svg>
									{:else}
										<svg viewBox="0 0 24 24" fill="currentColor" class="h-5 w-5">
											<path d="M8 5v14l11-7z" />
										</svg>
									{/if}
								</span>
							</Cover>
							<div class="mt-2.5 truncate text-sm font-semibold text-[var(--mf-text)]" title={song.title}>
								{song.title}
							</div>
							<div class="mt-0.5 flex min-w-0 items-center gap-1.5">
								{#if song.isExplicit}
									<span
										class="grid h-3.5 w-3.5 shrink-0 place-items-center rounded-[3px] bg-white/20 text-[9px] font-bold leading-none text-neutral-200"
										title="Contenido explícito"
									>
										E
									</span>
								{/if}
								<span class="truncate text-xs text-neutral-500" title={song.artist}>{song.artist}</span>
							</div>
						</button>
					</li>
				{/each}
			</ul>

			{#if ytContinuation}
				<div class="mt-8 flex justify-center">
					<button
						type="button"
						onclick={loadMoreYouTube}
						disabled={ytLoadingMore}
						class="flex items-center gap-2 rounded-full border border-white/15 px-6 py-3 text-sm font-semibold text-white transition hover:bg-white/5 disabled:opacity-50"
					>
						{#if ytLoadingMore}
							<LoaderCircle class="h-4 w-4 animate-spin" />
						{/if}
						Cargar más
					</button>
				</div>
			{/if}
		{/if}
	{/if}
</section>

{#if contextMenu}
	<div
		class="fixed inset-0 z-30"
		role="button"
		tabindex="0"
		onclick={closeContextMenu}
		onkeydown={(e) => {
			if (e.key === 'Escape') closeContextMenu();
		}}
		oncontextmenu={(e) => {
			e.preventDefault();
			closeContextMenu();
		}}
	></div>
	<div
		class="fixed z-40 w-60 rounded-xl border border-[var(--mf-border)] bg-[var(--mf-elevated)] p-1.5 shadow-[0_12px_28px_-10px_rgba(0,0,0,0.6)]"
		style="left:{contextMenu.x}px; top:{contextMenu.y}px;"
	>
		<button
			type="button"
			onclick={() => addToQueue(contextMenu)}
			class="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left text-sm font-semibold text-[var(--mf-text-2)] transition hover:bg-white/5"
		>
			<ListPlus class="h-[18px] w-[18px] shrink-0" strokeWidth={2} />
			<span>Añadir a la cola</span>
		</button>
		<div class="group/addmenu relative">
			<div
				class="flex items-center justify-between gap-3 rounded-lg px-3 py-2.5 text-sm font-semibold text-[var(--mf-text-2)] transition group-hover/addmenu:bg-white/5"
			>
				<span>Añadir a una playlist</span>
				<ChevronRight class="h-4 w-4 shrink-0 text-[var(--mf-text-3)]" />
			</div>
			<div
				class="invisible absolute top-0 z-50 opacity-0 transition group-hover/addmenu:visible group-hover/addmenu:opacity-100 {contextMenu.openLeft
					? 'right-full pr-1.5'
					: 'left-full pl-1.5'}"
			>
				<div
					class="w-64 overflow-hidden rounded-xl border border-[var(--mf-border)] bg-[var(--mf-elevated)] p-2 shadow-[0_12px_28px_-10px_rgba(0,0,0,0.6)]"
				>
					<div class="max-h-80 overflow-y-auto">
						{#each data.playlists as playlist (playlist.id)}
							<form
								method="POST"
								action={contextMenu.kind === 'youtube' ? '?/addYouTubeToPlaylist' : '?/addTrack'}
								use:enhance={() =>
									({ update }) => {
										closeContextMenu();
										return update({ reset: false });
									}}
							>
								<input type="hidden" name="playlistId" value={playlist.id} />
								{#if contextMenu.kind === 'youtube'}
									<input type="hidden" name="videoId" value={contextMenu.song.videoId} />
									<input type="hidden" name="title" value={contextMenu.song.title} />
									<input type="hidden" name="artist" value={contextMenu.song.artist} />
									<input type="hidden" name="durationSeconds" value={contextMenu.song.durationSeconds} />
									<input type="hidden" name="thumbnailUrl" value={contextMenu.song.thumbnailUrl} />
								{:else}
									<input type="hidden" name="trackId" value={contextMenu.track.id} />
								{/if}
								<button
									type="submit"
									class="flex w-full items-center gap-3 rounded-lg px-2 py-2.5 text-left text-base text-[var(--mf-text)] transition hover:bg-white/5"
								>
									<PlaylistArt
										playlistId={playlist.id}
										trackIds={[]}
										hue={hueFor(playlist.id)}
										size="small"
										class="h-11 w-11 flex-shrink-0 rounded-lg"
									/>
									<span class="truncate">{playlist.name}</span>
								</button>
							</form>
						{/each}
					</div>
				</div>
			</div>
		</div>
	</div>
{/if}
