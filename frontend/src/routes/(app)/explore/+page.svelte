<script lang="ts">
	import Search from '@lucide/svelte/icons/search';
	import Music from '@lucide/svelte/icons/music';
	import { goto } from '$app/navigation';
	import { player } from '$lib/player/player.svelte';
	import Cover from '$lib/components/ui/Cover.svelte';
	import { HUES } from '$lib/theme/color';

	let { data } = $props();

	const tracks = $derived(data.tracks);
	const currentPage = $derived(Number(tracks.pageNumber));
	const total = $derived(Number(tracks.totalItemCount));

	const dateFormatter = new Intl.DateTimeFormat('es', { dateStyle: 'medium' });

	let searchTimeout: ReturnType<typeof setTimeout> | undefined;

	function onSearchInput(event: Event) {
		const value = (event.currentTarget as HTMLInputElement).value;
		clearTimeout(searchTimeout);
		searchTimeout = setTimeout(() => {
			const params = new URLSearchParams();
			if (value.trim()) params.set('q', value.trim());
			const qs = params.toString();
			goto(qs ? `/explore?${qs}` : '/explore', { keepFocus: true, replaceState: true, noScroll: true });
		}, 350);
	}

	function hueFor(id: string) {
		let hash = 0;
		for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) >>> 0;
		return HUES[hash % HUES.length];
	}

	function togglePlay(index: number) {
		const track = tracks.items[index];
		if (player.current?.id === track.id) player.toggle();
		else player.playQueue(tracks.items, index);
	}

	function pageHref(page: number) {
		const params = new URLSearchParams();
		if (data.query) params.set('q', data.query);
		if (page > 1) params.set('page', String(page));
		const qs = params.toString();
		return qs ? `/explore?${qs}` : '/explore';
	}
</script>

<svelte:head>
	<title>Explorar · Musify</title>
	<meta name="description" content="Explora y busca canciones en Musify." />
</svelte:head>

<section class="px-8 py-12">
	<h1 class="font-display text-3xl font-bold tracking-tight sm:text-4xl">Explorar</h1>
	<p class="mt-2 text-neutral-400">Descubre canciones o busca por nombre.</p>

	<div class="relative mt-8">
		<span class="pointer-events-none absolute inset-y-0 left-4 grid place-items-center text-neutral-500">
			<Search class="h-4 w-4" />
		</span>
		<input
			type="search"
			value={data.query}
			oninput={onSearchInput}
			placeholder="Buscar por nombre…"
			autocomplete="off"
			class="w-full rounded-lg border border-white/15 bg-neutral-950 py-3 pl-11 pr-4 text-sm text-neutral-100 placeholder:text-neutral-500 focus:border-emerald-500/60 focus:outline-none"
		/>
	</div>

	<p class="mt-6 text-sm text-neutral-500">
		{#if data.query}
			{total}
			{total === 1 ? 'resultado' : 'resultados'} para «{data.query}»
		{:else}
			{total}
			{total === 1 ? 'canción' : 'canciones'}
		{/if}
	</p>

	{#if tracks.items.length === 0}
		<div class="mt-16 rounded-2xl border border-white/10 bg-neutral-900/40 p-12 text-center backdrop-blur-md">
			<Music class="mx-auto h-10 w-10 text-neutral-500" />
			<p class="mt-4 text-neutral-300">
				{#if data.query}
					No hay canciones que coincidan con «{data.query}».
				{:else}
					Todavía no hay canciones. ¡Sé el primero en subir una!
				{/if}
			</p>
		</div>
	{:else}
		<ul class="mt-8 grid grid-cols-[repeat(auto-fill,minmax(160px,1fr))] gap-4">
			{#each tracks.items as track, i (track.id)}
				<li
					class="group animate-enter rounded-2xl bg-neutral-900/40 p-3 transition duration-150 hover:bg-neutral-900/70"
					style="animation-delay:{i * 40}ms"
				>
					<Cover
						trackId={track.id}
						hue={hueFor(track.id)}
						size="large"
						alt={track.title}
						class="aspect-square w-full rounded-xl shadow-[0_12px_28px_-12px_rgba(0,0,0,0.7)]"
					>
						<button
							type="button"
							onclick={() => togglePlay(i)}
							aria-label={player.current?.id === track.id && player.isPlaying
								? 'Pausar'
								: 'Reproducir'}
							class="absolute inset-0 grid place-items-center bg-gradient-to-t from-black/50 to-transparent opacity-0 transition group-hover:opacity-100 focus-visible:opacity-100"
							class:opacity-100={player.current?.id === track.id}
						>
							<span
								class="grid h-12 w-12 translate-y-2 place-items-center rounded-full bg-[var(--mf-accent)] text-neutral-950 shadow-lg transition-transform group-hover:translate-y-0"
								class:!translate-y-0={player.current?.id === track.id}
							>
								{#if player.current?.id === track.id && player.isPlaying}
									<svg viewBox="0 0 24 24" fill="currentColor" class="h-6 w-6">
										<path d="M6 5h4v14H6zM14 5h4v14h-4z" />
									</svg>
								{:else}
									<svg viewBox="0 0 24 24" fill="currentColor" class="h-6 w-6">
										<path d="M8 5v14l11-7z" />
									</svg>
								{/if}
							</span>
						</button>
					</Cover>
					<h3 class="mt-3 truncate px-1 font-semibold" title={track.title}>{track.title}</h3>
					<p class="mt-0.5 px-1 text-xs text-neutral-500">
						{dateFormatter.format(new Date(track.createdAt))}
					</p>
				</li>
			{/each}
		</ul>

		{#if tracks.hasPreviousPage || tracks.hasNextPage}
			<nav class="mt-10 flex items-center justify-center gap-4">
				{#if tracks.hasPreviousPage}
					<a
						href={pageHref(currentPage - 1)}
						class="rounded-lg border border-white/20 px-5 py-2 text-sm font-medium text-white backdrop-blur-md transition hover:bg-white/5"
					>
						Anterior
					</a>
				{/if}
				<span class="text-sm text-neutral-500">Página {currentPage}</span>
				{#if tracks.hasNextPage}
					<a
						href={pageHref(currentPage + 1)}
						class="rounded-lg border border-white/20 px-5 py-2 text-sm font-medium text-white backdrop-blur-md transition hover:bg-white/5"
					>
						Siguiente
					</a>
				{/if}
			</nav>
		{/if}
	{/if}
</section>
