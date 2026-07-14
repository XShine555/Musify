<script lang="ts">
	import { enhance } from '$app/forms';
	import ArrowLeft from '@lucide/svelte/icons/arrow-left';
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import Pencil from '@lucide/svelte/icons/pencil';
	import Trash2 from '@lucide/svelte/icons/trash-2';
	import Plus from '@lucide/svelte/icons/plus';
	import X from '@lucide/svelte/icons/x';
	import Music from '@lucide/svelte/icons/music';
	import Clock from '@lucide/svelte/icons/clock';
	import { player } from '$lib/player/player.svelte';
	import { HUES, fmtTime } from '$lib/theme/color';
	import Cover from '$lib/components/ui/Cover.svelte';
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
	import NowPlaying from '$lib/components/ui/NowPlaying.svelte';

	let { data, form } = $props();

	const playlist = $derived(data.playlist);
	const tracks = $derived(data.tracks);
	const library = $derived(data.library);

	let editing = $state(false);
	let confirmingDelete = $state(false);

	const dateFormatter = new Intl.DateTimeFormat('es', { dateStyle: 'medium' });

	function hueFor(id: string) {
		let hash = 0;
		for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) >>> 0;
		return HUES[hash % HUES.length];
	}

	const isCurrentQueue = $derived(tracks.some((t) => t.id === player.current.id));

	function playAll() {
		if (tracks.length === 0) return;
		if (isCurrentQueue) player.toggle();
		else player.playQueue(tracks, 0);
	}

	function playFrom(index: number) {
		const track = tracks[index];
		if (player.current.id === track.id) player.toggle();
		else player.playQueue(tracks, index);
	}
</script>

<svelte:head>
	<title>{playlist.name} · Musify</title>
	<meta name="description" content="Playlist {playlist.name} en Musify." />
</svelte:head>

<section class="px-8 py-8">
	<a
		href="/playlists"
		class="group inline-flex items-center gap-2 text-sm font-medium text-neutral-400 transition-colors hover:text-white"
	>
		<span
			class="grid h-8 w-8 place-items-center rounded-lg border border-white/15 transition-all duration-200 group-hover:-translate-x-0.5 group-hover:border-emerald-500/60 group-hover:text-emerald-400"
		>
			<ArrowLeft class="h-4 w-4" />
		</span>
		Mis listas
	</a>

	<div class="mt-6 flex flex-col gap-6 sm:flex-row sm:items-end">
		<PlaylistArt
			trackIds={tracks.map((t) => t.id)}
			hue={hueFor(playlist.id)}
			class="h-44 w-44 flex-shrink-0 rounded-2xl shadow-[0_20px_45px_-15px_rgba(0,0,0,0.7)]"
		/>
		<div class="min-w-0 flex-1">
			<p class="text-xs font-semibold uppercase tracking-[0.14em] text-neutral-400">Lista</p>
			<h1 class="mt-1.5 font-display text-4xl font-bold tracking-tight sm:text-5xl">{playlist.name}</h1>
			{#if playlist.description}
				<p class="mt-3 max-w-2xl text-neutral-300">{playlist.description}</p>
			{/if}
			<p class="mt-3 text-sm text-neutral-500">
				{tracks.length}
				{tracks.length === 1 ? 'canción' : 'canciones'}
			</p>
		</div>
	</div>

	<div class="mt-7 flex items-center gap-3">
		<button
			type="button"
			onclick={playAll}
			disabled={tracks.length === 0}
			class="inline-flex items-center gap-2 rounded-lg bg-[var(--mf-accent)] px-7 py-3 text-sm font-semibold text-neutral-950 transition hover:brightness-110 hover:shadow-[0_10px_28px_-8px] active:scale-95 disabled:opacity-40"
		>
			{#if isCurrentQueue && player.playing}
				<Pause class="h-4 w-4" fill="currentColor" />
				Pausar
			{:else}
				<Play class="h-4 w-4" fill="currentColor" />
				Reproducir
			{/if}
		</button>
		<button
			type="button"
			onclick={() => (editing = true)}
			class="inline-flex items-center gap-2 rounded-lg border border-white/15 px-5 py-3 text-sm font-semibold text-white transition hover:bg-white/5"
		>
			<Pencil class="h-4 w-4" />
			Editar
		</button>
		<button
			type="button"
			onclick={() => (confirmingDelete = true)}
			aria-label="Eliminar playlist"
			class="inline-flex items-center gap-2 rounded-lg border border-white/15 px-5 py-3 text-sm font-semibold text-neutral-300 transition hover:border-red-500/40 hover:bg-red-500/10 hover:text-red-200"
		>
			<Trash2 class="h-4 w-4" />
		</button>
	</div>

	{#if tracks.length > 0}
		<div class="mt-8">
			<div
				class="grid grid-cols-[32px_1fr_140px_100px] items-center gap-4 border-b border-white/10 px-3 pb-2 text-xs font-semibold uppercase tracking-wide text-neutral-500"
			>
				<span class="text-center">#</span>
				<span>Título</span>
				<span>Añadida</span>
				<span class="flex justify-end pr-1"><Clock class="h-3.5 w-3.5" /></span>
			</div>
			<div class="mt-1 flex flex-col">
				{#each tracks as track, i (track.id)}
					<div
						class="group grid grid-cols-[32px_1fr_140px_100px] items-center gap-4 rounded-[10px] px-3 py-[9px] transition hover:bg-neutral-900/60"
					>
						<button
							type="button"
							onclick={() => playFrom(i)}
							aria-label={player.current.id === track.id && player.playing ? 'Pausar' : 'Reproducir'}
							class="grid h-8 w-8 place-items-center text-sm text-neutral-500"
						>
							{#if player.current.id === track.id}
								<NowPlaying paused={!player.playing} />
							{:else}
								<span class="group-hover:hidden">{i + 1}</span>
								<Play class="hidden h-3.5 w-3.5 text-white group-hover:block" fill="currentColor" />
							{/if}
						</button>
						<button type="button" onclick={() => playFrom(i)} class="flex min-w-0 items-center gap-3.5 text-left">
							<Cover
								trackId={track.id}
								hue={hueFor(track.id)}
								size="small"
								alt={track.title}
								class="h-[42px] w-[42px] flex-shrink-0 rounded-md"
							/>
							<div class="min-w-0 truncate text-[14.5px] font-semibold text-neutral-100">{track.title}</div>
						</button>
						<div class="truncate text-[12.5px] text-neutral-500">
							{dateFormatter.format(new Date(track.createdAt))}
						</div>
						<div class="flex items-center justify-end gap-2">
							<form method="POST" action="?/removeTrack" use:enhance={() => async ({ update }) => update()}>
								<input type="hidden" name="trackId" value={track.id} />
								<button
									type="submit"
									aria-label="Quitar de la playlist"
									class="grid h-7 w-7 place-items-center rounded-full text-neutral-500 opacity-0 transition hover:bg-white/5 hover:text-white group-hover:opacity-100"
								>
									<X class="h-3.5 w-3.5" />
								</button>
							</form>
							<span class="text-[12.5px] tabular-nums text-neutral-500">{fmtTime(Number(track.duration))}</span>
						</div>
					</div>
				{/each}
			</div>
		</div>
	{:else}
		<div class="mt-8 max-w-[900px] rounded-2xl border border-white/10 bg-neutral-900/40 p-10 text-center">
			<Music class="mx-auto h-9 w-9 text-neutral-600" />
			<p class="mt-4 text-neutral-300">Esta playlist está vacía. Añade canciones de tu biblioteca.</p>
		</div>
	{/if}

	{#if library.length > 0}
		<div class="mt-12 max-w-[900px]">
			<h2 class="font-display text-lg font-bold tracking-tight">Añadir de tu biblioteca</h2>
			<div class="mt-4 flex flex-col">
				{#each library as track (track.id)}
					<div class="flex items-center gap-3.5 rounded-[10px] px-3 py-[10px] transition hover:bg-neutral-900/60">
						<Cover
							trackId={track.id}
							hue={hueFor(track.id)}
							size="small"
							alt={track.title}
							class="h-[40px] w-[40px] flex-shrink-0 rounded-md"
						/>
						<div class="min-w-0 flex-1 truncate text-sm font-medium text-neutral-200">{track.title}</div>
						<form method="POST" action="?/addTrack" use:enhance={() => async ({ update }) => update()}>
							<input type="hidden" name="trackId" value={track.id} />
							<button
								type="submit"
								class="inline-flex items-center gap-1.5 rounded-lg border border-white/15 px-4 py-2 text-xs font-semibold text-white transition hover:border-emerald-500/50 hover:bg-emerald-500/10 hover:text-emerald-300"
							>
								<Plus class="h-3.5 w-3.5" strokeWidth={2.5} />
								Añadir
							</button>
						</form>
					</div>
				{/each}
			</div>
		</div>
	{/if}
</section>

{#if editing}
	<div
		class="animate-fade fixed inset-0 z-[60] grid place-items-center bg-black/70 p-4 backdrop-blur-sm"
		role="button"
		tabindex="0"
		onclick={(e) => {
			if (e.target === e.currentTarget) editing = false;
		}}
		onkeydown={(e) => {
			if (e.key === 'Escape') editing = false;
		}}
	>
		<div class="animate-pop w-full max-w-md rounded-2xl border border-white/10 bg-neutral-900 p-6 shadow-2xl">
			<div class="flex items-center justify-between">
				<h2 class="font-display text-lg font-bold tracking-tight">Editar playlist</h2>
				<button
					type="button"
					onclick={() => (editing = false)}
					aria-label="Cerrar"
					class="grid h-8 w-8 place-items-center rounded-full text-neutral-400 transition hover:bg-white/5 hover:text-white"
				>
					<X class="h-4 w-4" />
				</button>
			</div>
			<form
				method="POST"
				action="?/rename"
				use:enhance={() => async ({ update, result }) => {
					await update({ reset: false });
					if (result.type === 'success') editing = false;
				}}
				class="mt-5 space-y-4"
			>
				<div>
					<label for="edit-name" class="mb-2 block text-sm font-medium text-neutral-300">Nombre</label>
					<input
						id="edit-name"
						name="name"
						type="text"
						maxlength={100}
						required
						value={playlist.name}
						class="w-full rounded-xl border border-white/10 bg-neutral-950 px-4 py-3 text-sm text-neutral-100 focus:border-emerald-500/60 focus:outline-none"
					/>
				</div>
				<div>
					<label for="edit-desc" class="mb-2 block text-sm font-medium text-neutral-300">Descripción</label>
					<textarea
						id="edit-desc"
						name="description"
						rows="3"
						maxlength={300}
						value={playlist.description}
						class="w-full resize-none rounded-xl border border-white/10 bg-neutral-950 px-4 py-3 text-sm text-neutral-100 focus:border-emerald-500/60 focus:outline-none"
					></textarea>
				</div>
				{#if form?.message}
					<p class="rounded-lg border border-red-500/40 bg-red-500/10 px-3 py-2 text-sm text-red-200">
						{form.message}
					</p>
				{/if}
				<div class="flex items-center justify-end gap-3 pt-1">
					<button
						type="button"
						onclick={() => (editing = false)}
						class="rounded-lg border border-white/15 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-white/5"
					>
						Cancelar
					</button>
					<button
						type="submit"
						class="rounded-lg bg-[var(--mf-accent)] px-6 py-2.5 text-sm font-semibold text-neutral-950 transition hover:brightness-110"
					>
						Guardar
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

{#if confirmingDelete}
	<div
		class="animate-fade fixed inset-0 z-[60] grid place-items-center bg-black/70 p-4 backdrop-blur-sm"
		role="button"
		tabindex="0"
		onclick={(e) => {
			if (e.target === e.currentTarget) confirmingDelete = false;
		}}
		onkeydown={(e) => {
			if (e.key === 'Escape') confirmingDelete = false;
		}}
	>
		<div class="animate-pop w-full max-w-sm rounded-2xl border border-white/10 bg-neutral-900 p-6 shadow-2xl">
			<h2 class="font-display text-lg font-bold tracking-tight">¿Eliminar «{playlist.name}»?</h2>
			<p class="mt-2 text-sm text-neutral-400">
				Esta acción no se puede deshacer. Las canciones seguirán en tu biblioteca.
			</p>
			<div class="mt-6 flex items-center justify-end gap-3">
				<button
					type="button"
					onclick={() => (confirmingDelete = false)}
					class="rounded-lg border border-white/15 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-white/5"
				>
					Cancelar
				</button>
				<form method="POST" action="?/delete">
					<button
						type="submit"
						class="rounded-lg bg-red-500 px-6 py-2.5 text-sm font-semibold text-white transition hover:bg-red-400"
					>
						Eliminar
					</button>
				</form>
			</div>
		</div>
	</div>
{/if}
