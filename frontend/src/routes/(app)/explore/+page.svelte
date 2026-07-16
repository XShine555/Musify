<script lang="ts">
	import Search from '@lucide/svelte/icons/search';
	import Music from '@lucide/svelte/icons/music';
	import ListPlus from '@lucide/svelte/icons/list-plus';
	import Check from '@lucide/svelte/icons/check';
	import LoaderCircle from '@lucide/svelte/icons/loader-circle';
	import { goto } from '$app/navigation';
	import { enhance } from '$app/forms';
	import { player, queueIdForTrack, toQueueItems } from '$lib/player/player.svelte';
	import Cover from '$lib/components/ui/Cover.svelte';
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

	type Source = 'all' | 'local' | 'yt';

	let { data, form } = $props();

	let searchTimeout: ReturnType<typeof setTimeout> | undefined;

	let ytItems = $state<YouTubeSong[]>([]);
	let ytContinuation = $state('');
	let ytLoadingMore = $state(false);
	let addedVideoIds = $state<Set<string>>(new Set());

	$effect(() => {
		ytItems = (data.ytResults?.items ?? []) as YouTubeSong[];
		ytContinuation = data.ytResults?.continuationToken ?? '';
		addedVideoIds = new Set();
	});

	const showLocal = $derived(data.tracks !== null);
	const showYouTube = $derived(data.src !== 'local' && !!data.query);
	const showHeadings = $derived(data.src === 'all' && !!data.query);

	function buildHref(src: Source, page = 1, query = data.query) {
		const params = new URLSearchParams();
		if (query) params.set('q', query);
		if (src !== 'all') params.set('src', src);
		if (page > 1) params.set('page', String(page));
		const qs = params.toString();
		return qs ? `/explore?${qs}` : '/explore';
	}

	function onSearchInput(event: Event) {
		const value = (event.currentTarget as HTMLInputElement).value;
		clearTimeout(searchTimeout);
		searchTimeout = setTimeout(() => {
			goto(buildHref(data.src, 1, value.trim()), {
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
		if (!data.tracks) return;
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

	const tabs: { src: Source; label: string }[] = [
		{ src: 'all', label: 'Todo' },
		{ src: 'local', label: 'Mi música' },
		{ src: 'yt', label: 'YouTube Music' }
	];

	const placeholders: Record<Source, string> = {
		all: 'Buscar en tu música y en YouTube Music...',
		local: 'Buscar por nombre...',
		yt: 'Buscar en YouTube Music...'
	};
</script>

<svelte:head>
	<title>Explorar · Musify</title>
	<meta name="description" content="Explora tu música y busca canciones en YouTube Music." />
</svelte:head>

<section class="px-8 pt-12 pb-8">
	<h1 class="font-display text-3xl font-bold tracking-tight sm:text-4xl">Explorar</h1>
	<p class="mt-2 text-neutral-400">Busca a la vez en tu música y en YouTube Music, o filtra por fuente.</p>

	<div class="mt-8 flex items-center gap-2">
		{#each tabs as tab (tab.src)}
			<a
				href={buildHref(tab.src)}
				data-sveltekit-noscroll
				class="rounded-full px-4 py-1.5 text-sm transition {data.src === tab.src
					? 'bg-emerald-500 font-semibold text-neutral-950'
					: 'border border-white/15 text-neutral-300 hover:bg-white/5'}"
			>
				{tab.label}
			</a>
		{/each}
	</div>

	<div class="relative mt-6">
		<span class="pointer-events-none absolute inset-y-0 left-4 grid place-items-center text-neutral-500">
			<Search class="h-4 w-4" />
		</span>
		<input
			type="search"
			value={data.query}
			oninput={onSearchInput}
			placeholder={placeholders[data.src]}
			autocomplete="off"
			class="w-full rounded-lg border border-white/15 bg-neutral-950 py-3 pl-11 pr-4 text-sm text-neutral-100 placeholder:text-neutral-500 focus:border-[var(--mf-accent)]/50 focus:outline-none"
		/>
	</div>

	{#if form?.message}
		<p class="mt-4 rounded-xl border border-white/10 bg-neutral-900/40 px-4 py-3 text-sm text-neutral-300">
			{form.message}
		</p>
	{/if}

	{#if showLocal && data.tracks}
		{@const tracks = data.tracks}
		{@const currentPage = Number(tracks.pageNumber)}
		{@const total = Number(tracks.totalItemCount)}

		{#if showHeadings}
			<h2 class="mt-10 font-display text-lg font-bold tracking-tight">Mi música</h2>
		{/if}

		<p class="{showHeadings ? 'mt-1' : 'mt-6'} text-sm text-neutral-500">
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
				<p class="mt-3 text-sm text-neutral-300">
					{#if data.query}
						No hay canciones tuyas que coincidan con «{data.query}».
					{:else}
						Todavía no hay canciones. ¡Sé el primero en subir una!
					{/if}
				</p>
			</div>
		{:else}
			<ul class="mt-6 grid grid-cols-[repeat(auto-fill,minmax(160px,1fr))] gap-4">
				{#each tracks.items as track, i (track.id)}
					<li class="group animate-enter" style="animation-delay:{i * 40}ms">
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
							href={buildHref(data.src, currentPage - 1)}
							class="rounded-lg border border-white/10 px-5 py-2 text-sm text-white/65 transition hover:bg-white/2.5"
						>
							Anterior
						</a>
					{/if}
					<span class="text-sm text-neutral-500">Página {currentPage}</span>
					{#if tracks.hasNextPage}
						<a
							href={buildHref(data.src, currentPage + 1)}
							class="rounded-lg border border-white/10 px-5 py-2 text-sm text-white/65 transition hover:bg-white/2.5"
						>
							Siguiente
						</a>
					{/if}
				</nav>
			{/if}
		{/if}
	{/if}

	{#if data.src !== 'local'}
		{#if showHeadings}
			<h2 class="mt-12 font-display text-lg font-bold tracking-tight">YouTube Music</h2>
		{/if}

		{#if data.needsAuth}
			{#if data.query || data.src === 'yt'}
				<div class="mt-6 rounded-2xl border border-white/10 bg-neutral-900/40 p-10 text-center backdrop-blur-md">
					<Music class="mx-auto h-8 w-8 text-neutral-500" />
					<p class="mt-3 text-sm text-neutral-300">Inicia sesión para buscar en YouTube Music.</p>
				</div>
			{/if}
		{:else if !data.query}
			{#if data.src === 'yt'}
				<div class="mt-16 rounded-2xl border border-white/10 bg-neutral-900/40 p-12 text-center backdrop-blur-md">
					<Search class="mx-auto h-10 w-10 text-neutral-500" />
					<p class="mt-4 text-neutral-300">Escribe algo para buscar canciones en YouTube Music.</p>
				</div>
			{/if}
		{:else if data.ytError}
			<div class="mt-6 rounded-2xl border border-white/10 bg-neutral-900/40 p-10 text-center backdrop-blur-md">
				<Music class="mx-auto h-8 w-8 text-neutral-500" />
				<p class="mt-3 text-sm text-neutral-300">YouTube Music no está disponible ahora mismo. Inténtalo de nuevo.</p>
			</div>
		{:else if showYouTube}
			{#if ytItems.length === 0}
				<div class="mt-6 rounded-2xl border border-white/10 bg-neutral-900/40 p-10 text-center backdrop-blur-md">
					<Music class="mx-auto h-8 w-8 text-neutral-500" />
					<p class="mt-3 text-sm text-neutral-300">No hay resultados en YouTube Music para «{data.query}».</p>
				</div>
			{:else}
				<ul class="mt-6 grid grid-cols-[repeat(auto-fill,minmax(160px,1fr))] gap-4">
					{#each ytItems as song, i (song.videoId)}
						<li class="group animate-enter relative" style="animation-delay:{i * 40}ms">
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

							{#if data.playlists.length > 0}
								<details class="absolute right-2.5 top-2.5 z-10">
									<summary
										class="grid h-9 w-9 cursor-pointer list-none place-items-center rounded-full bg-neutral-950/70 text-white opacity-0 shadow-lg backdrop-blur-sm transition group-hover:opacity-100 [&::-webkit-details-marker]:hidden"
										class:!opacity-100={addedVideoIds.has(song.videoId)}
										title="Añadir a playlist"
									>
										{#if addedVideoIds.has(song.videoId)}
											<Check class="h-4 w-4 text-emerald-400" />
										{:else}
											<ListPlus class="h-4 w-4" />
										{/if}
									</summary>
									<div
										class="absolute right-0 z-20 mt-2 w-56 rounded-2xl border border-white/10 bg-neutral-900 p-2 shadow-[0_12px_28px_-10px_rgba(0,0,0,0.8)]"
									>
										<p class="px-3 py-1.5 text-xs font-semibold text-neutral-500">Añadir a playlist</p>
										{#each data.playlists as playlist (playlist.id)}
											<form
												method="POST"
												action="?/addYouTubeToPlaylist"
												use:enhance={() =>
													({ result, update }) => {
														if (result.type === 'success') {
															addedVideoIds = new Set([...addedVideoIds, song.videoId]);
														}
														return update({ reset: false });
													}}
											>
												<input type="hidden" name="playlistId" value={playlist.id} />
												<input type="hidden" name="videoId" value={song.videoId} />
												<input type="hidden" name="title" value={song.title} />
												<input type="hidden" name="artist" value={song.artist} />
												<input type="hidden" name="durationSeconds" value={song.durationSeconds} />
												<input type="hidden" name="thumbnailUrl" value={song.thumbnailUrl} />
												<button
													type="submit"
													class="w-full truncate rounded-lg px-3 py-2 text-left text-sm text-neutral-200 transition hover:bg-white/5"
												>
													{playlist.name}
												</button>
											</form>
										{/each}
									</div>
								</details>
							{/if}
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
	{/if}
</section>
