<script lang="ts">
	import { enhance } from '$app/forms';
	import Upload from '@lucide/svelte/icons/upload';
	import Play from '@lucide/svelte/icons/play';
	import X from '@lucide/svelte/icons/x';
	import { player, queueIdForTrack, toQueueItems } from '$lib/player/player.svelte';
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
		if (player.current.id === queueIdForTrack(track)) player.toggle();
		else player.playQueue(toQueueItems(items), index);
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
				class="grid grid-cols-[32px_1fr_100px_100px_64px_36px] items-center gap-8 border-b border-white/10 px-3 pb-2 text-xs uppercase tracking-wide text-neutral-500"
			>
				<span class="text-center">#</span>
				<span>Título</span>
				<span class="text-center">Subida</span>
				<span class="text-center">Escuchas</span>
				<span class="text-center">Duración</span>
				<span></span>
			</div>
			<div class="mt-1 flex flex-col">
				{#each items as track, i (track.id)}
					<div
						class="group grid grid-cols-[32px_1fr_100px_100px_64px_36px] items-center gap-8 rounded-[10px] px-3 py-[9px] transition hover:bg-neutral-900/60"
					>
						<button
							type="button"
							onclick={() => togglePlay(i)}
							aria-label={player.current.id === track.id && player.playing ? 'Pausar' : 'Reproducir'}
							class="relative grid h-8 w-8 place-items-center overflow-hidden rounded text-sm text-neutral-500"
						>
							{#if player.current.id === track.id}
								<NowPlaying paused={!player.playing} />
							{:else}
								<span class="group-hover:hidden">{i + 1}</span>
								<Play class="hidden h-3.5 w-3.5 text-white group-hover:block" fill="currentColor" />
							{/if}
						</button>
						<button
							type="button"
							onclick={() => togglePlay(i)}
							class="flex min-w-0 items-center gap-3.5 text-left"
						>
							<Cover
								trackId={track.id}
								hue={hueFor(track.id)}
								size="small"
								alt={track.title}
								class="h-[42px] w-[42px] flex-shrink-0 rounded-md"
							/>
							<div class="min-w-0 truncate text-[14.5px] font-semibold text-neutral-100">{track.title}</div>
						</button>
						<div class="truncate text-center text-[12.5px] text-neutral-500">
							{dateFormatter.format(new Date(track.createdAt))}
						</div>
						<span class="text-center text-[12.5px] tabular-nums text-neutral-500">
							{Number(track.listensCount)}
						</span>
						<span class="text-center text-[12.5px] tabular-nums text-neutral-500">
							{fmtTime(Number(track.duration))}
						</span>
						<form method="POST" action="?/deleteTrack" use:enhance={() => async ({ update }) => update()}>
							<input type="hidden" name="trackId" value={track.id} />
							<button
								type="submit"
								aria-label="Borrar canción"
								class="grid h-7 w-7 place-items-center rounded-lg text-neutral-500 opacity-0 transition hover:bg-white/5 hover:text-white group-hover:opacity-100"
							>
								<X class="h-3.5 w-3.5" />
							</button>
						</form>
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
