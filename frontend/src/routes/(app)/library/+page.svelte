<script lang="ts">
	import Upload from '@lucide/svelte/icons/upload';
	import Headphones from '@lucide/svelte/icons/headphones';
	import Clock from '@lucide/svelte/icons/clock';
	import { player } from '$lib/player/player.svelte';
	import { HUES, fmtTime } from '$lib/theme/color';
	import Cover from '$lib/components/ui/Cover.svelte';
	import NowPlaying from '$lib/components/ui/NowPlaying.svelte';

	let { data } = $props();

	const items = $derived(data.tracks.items);
	const total = $derived(Number(data.tracks.totalItemCount));

	const dateFormatter = new Intl.DateTimeFormat('es', { dateStyle: 'medium' });

	function hueFor(id: string) {
		let hash = 0;
		for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) >>> 0;
		return HUES[hash % HUES.length];
	}

	function togglePlay(index: number) {
		const track = items[index];
		if (player.current.id === track.id) player.toggle();
		else player.playQueue(items, index);
	}
</script>

<svelte:head>
	<title>Tus canciones subidas · Musify</title>
	<meta name="description" content="Todas tus canciones subidas a Musify." />
</svelte:head>

<section class="px-8 pt-11 pb-8">
	<div class="flex items-end justify-between gap-4">
		<div>
			<h1 class="font-display text-3xl font-bold tracking-tight sm:text-4xl">Tus canciones subidas</h1>
			<p class="mt-2 text-[var(--mf-text-2)]">
				{total}
				{total === 1 ? 'canción' : 'canciones'} subida{total === 1 ? '' : 's'}.
			</p>
		</div>
		<a
			href="/upload"
			class="inline-flex items-center gap-2 rounded-lg bg-[var(--mf-accent)] px-5 py-2.5 text-sm font-semibold text-neutral-950 transition hover:brightness-110 hover:shadow-[0_8px_24px_-8px] active:scale-[0.97]"
		>
			<Upload class="h-4 w-4" />
			Subir música
		</a>
	</div>

	{#if items.length > 0}
		<div class="mt-8">
			<div
				class="grid grid-cols-[1fr_120px_160px] items-center gap-6 border-b border-[var(--mf-border)] px-3 pb-3 text-xs font-semibold uppercase tracking-wide text-[var(--mf-text-3)]"
			>
				<span>Título</span>
				<span class="flex items-center justify-end gap-1.5"><Clock class="h-3.5 w-3.5" /> Duración</span>
				<span class="flex items-center gap-1.5"><Headphones class="h-3.5 w-3.5" /> Escuchas</span>
			</div>
			<div class="mt-2 flex flex-col">
				{#each items as track, i (track.id)}
					<div
						class="group grid animate-enter grid-cols-[1fr_120px_160px] items-center gap-6 rounded-[10px] px-3 py-4 transition duration-100 hover:bg-[var(--mf-surface-hover)]"
						style="animation-delay:{i * 35}ms"
					>
						<button
							type="button"
							onclick={() => togglePlay(i)}
							class="flex min-w-0 items-center gap-3.5 text-left active:scale-[0.99]"
						>
							<Cover
								trackId={track.id}
								hue={hueFor(track.id)}
								size="small"
								alt={track.title}
								class="h-[46px] w-[46px] flex-shrink-0 rounded-md"
							>
								{#if player.current.id === track.id}
									<NowPlaying paused={!player.playing} />
								{/if}
							</Cover>
							<div class="min-w-0 flex-1">
								<div class="truncate text-[14.5px] font-semibold text-[var(--mf-text)]">
									{track.title}
								</div>
								<div class="truncate text-[12.5px] text-[var(--mf-text-3)]">
									{dateFormatter.format(new Date(track.createdAt))}
								</div>
							</div>
						</button>
						<span class="text-right text-[12.5px] tabular-nums text-[var(--mf-text-3)]">
							{fmtTime(Number(track.duration))}
						</span>
						<div class="flex items-center gap-1.5 text-[12.5px] tabular-nums text-[var(--mf-text-3)]">
							<Headphones class="h-3.5 w-3.5" />
							{Number(track.listensCount)}
						</div>
					</div>
				{/each}
			</div>
		</div>
	{:else}
		<div
			class="mt-7 max-w-[560px] rounded-xl border border-[var(--mf-border)] bg-[var(--mf-surface)] px-6 py-10 text-center"
		>
			<p class="text-sm text-[var(--mf-text-2)]">Todavía no tienes canciones</p>
			<a
				href="/upload"
				class="mt-4 inline-flex items-center gap-2 rounded-lg bg-[var(--mf-accent)] px-5 py-2.5 text-sm font-semibold text-neutral-950 transition hover:brightness-110 hover:shadow-[0_8px_24px_-8px] active:scale-[0.97]"
			>
				<Upload class="h-4 w-4" />
				Sube la primera
			</a>
		</div>
	{/if}
</section>
