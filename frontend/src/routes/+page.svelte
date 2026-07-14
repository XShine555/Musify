<script lang="ts">
	import Music from '@lucide/svelte/icons/music';
	import { player } from '$lib/player/player.svelte';
	import { HUES } from '$lib/theme/color';
	import Cover from '$lib/components/ui/Cover.svelte';
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
	import NowPlaying from '$lib/components/ui/NowPlaying.svelte';

	let { data } = $props();

	const latest = $derived(data.latest);
	const playlists = $derived(data.playlists);
	const trackIds = $derived(data.trackIds);
	const recentlyPlayed = $derived.by(() => {
		const seen = new Set<string>();
		const merged: { id: string; title: string }[] = [];
		for (const t of player.recentlyPlayed) {
			const id = String(t.id);
			if (seen.has(id)) continue;
			seen.add(id);
			merged.push({ id, title: t.title });
		}
		for (const t of data.recentlyPlayed) {
			if (seen.has(t.id)) continue;
			seen.add(t.id);
			merged.push({ id: t.id, title: t.title });
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

	function hueFor(id: string) {
		let hash = 0;
		for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) >>> 0;
		return HUES[hash % HUES.length];
	}

	function playLatest(index: number) {
		const track = latest[index];
		if (player.current.id === track.id) player.toggle();
		else player.playQueue(latest, index);
	}

	function playRecent(index: number) {
		const track = recentlyPlayed[index];
		if (player.current.id === track.id) player.toggle();
		else player.playQueue(recentlyPlayed, index);
	}
</script>

<svelte:head>
	<title>Musify</title>
	<meta name="description" content="Tu música, sin límites." />
</svelte:head>

<section class="relative overflow-hidden px-8 pb-16 pt-11">
	<div
		class="pointer-events-none absolute inset-0 blur-[70px] saturate-150"
		style="background:linear-gradient(135deg, var(--mf-accent), color-mix(in oklch, var(--mf-accent), black 55%));animation:breathe 9s ease-in-out infinite"
	></div>
	<div class="animate-enter relative flex flex-col gap-2.5">
		<p class="text-[13px] uppercase tracking-[0.14em] text-[var(--mf-text-3)]">{greeting}</p>
		<h1 class="max-w-[640px] font-display text-[42px] font-extrabold leading-[1.05] tracking-tight">
			Tu música. Sin límites.
		</h1>
	</div>
</section>

{#if recentlyPlayed.length > 0}
	<section class="px-8 pb-3 pt-6">
		<h2 class="mb-4 font-display text-2xl font-bold">Escuchado recientemente</h2>
		<div class="grid grid-cols-[repeat(auto-fill,minmax(300px,1fr))] gap-4">
			{#each recentlyPlayed as track, i (track.id)}
				<button
					type="button"
					onclick={() => playRecent(i)}
					class="flex items-center gap-4 rounded-xl bg-[var(--mf-surface)] p-2.5 text-left transition hover:bg-[var(--mf-surface-hover)]"
				>
					<Cover
						trackId={track.id}
						hue={hueFor(track.id)}
						size="medium"
						alt={track.title}
						class="h-16 w-16 flex-shrink-0 rounded-lg"
					>
						{#if player.current.id === track.id}
							<NowPlaying paused={!player.playing} />
						{/if}
					</Cover>
					<div class="min-w-0">
						<div class="truncate text-base font-semibold text-[var(--mf-text)]">{track.title}</div>
					</div>
				</button>
			{/each}
		</div>
	</section>
{/if}

<section class="px-8 pb-3 pt-3">
	<h2 class="mb-4 font-display text-xl font-bold">Novedades</h2>
	{#if latest.length > 0}
		<div class="grid max-w-[1400px] grid-cols-[repeat(auto-fill,minmax(160px,1fr))] gap-4">
			{#each latest as track, i (track.id)}
				<button
					type="button"
					onclick={() => playLatest(i)}
					class="group animate-enter text-left"
					style="animation-delay:{i * 45}ms"
				>
					<Cover
						trackId={track.id}
						hue={hueFor(track.id)}
						size="large"
						alt={track.title}
						class="aspect-square w-full rounded-xl shadow-[0_12px_28px_-10px_rgba(0,0,0,0.6)]"
					>
						<span
							class="absolute right-2.5 bottom-2.5 grid h-11 w-11 translate-y-2 place-items-center rounded-full bg-[var(--mf-accent)] text-neutral-950 opacity-0 shadow-lg transition-all group-hover:translate-y-0 group-hover:opacity-100"
							class:!opacity-100={player.current.id === track.id}
							class:!translate-y-0={player.current.id === track.id}
						>
							{#if player.current.id === track.id && player.playing}
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
					<div class="mt-2.5 truncate text-sm font-semibold text-[var(--mf-text)]">{track.title}</div>
				</button>
			{/each}
		</div>
	{:else}
		<div class="max-w-[1400px] rounded-2xl border border-[var(--mf-border)] px-8 py-12 text-center">
			<Music class="mx-auto h-9 w-9 text-[var(--mf-text-4)]" />
			<h3 class="mt-4 font-display text-xl font-bold">Todavía no hay música</h3>
			<p class="mt-2 text-sm text-[var(--mf-text-2)]">
				<a href="/upload" class="text-emerald-400 underline underline-offset-2">Sube una canción</a>
				y empieza a escuchar.
			</p>
		</div>
	{/if}
</section>

<section class="px-8 pb-16 pt-3">
	<h2 class="mb-4 font-display text-xl font-bold">Mis listas</h2>
	{#if playlists.length > 0}
		<div class="grid max-w-[1400px] grid-cols-[repeat(auto-fill,minmax(150px,1fr))] gap-4">
			{#each playlists as playlist, i (playlist.id)}
				<a
					href="/playlists/{playlist.id}"
					class="group animate-enter text-left"
					style="animation-delay:{i * 45}ms"
				>
					<PlaylistArt
						trackIds={trackIds[playlist.id] ?? []}
						hue={hueFor(playlist.id)}
						class="aspect-square w-full rounded-xl shadow-[0_12px_28px_-10px_rgba(0,0,0,0.6)] transition group-hover:shadow-[0_16px_36px_-10px_rgba(0,0,0,0.75)]"
					/>
					<div class="mt-2.5 truncate text-sm font-semibold text-[var(--mf-text)]">
						{playlist.name}
					</div>
				</a>
			{/each}
		</div>
	{:else}
		<div
			class="max-w-[560px] rounded-xl border border-[var(--mf-border)] bg-[var(--mf-surface)] px-6 py-10 text-center"
		>
			<p class="text-sm text-[var(--mf-text-2)]">Todavía no tienes listas</p>
			<p class="mt-1.5 text-xs text-[var(--mf-text-4)]">
				Cuando crees listas, tus últimas aparecerán aquí.
			</p>
		</div>
	{/if}
</section>
