<script lang="ts">
	import { enhance } from '$app/forms';
	import ArrowLeft from '@lucide/svelte/icons/arrow-left';
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';
	import SquarePause from '@lucide/svelte/icons/square-pause';
	import Pencil from '@lucide/svelte/icons/pencil';
	import SquarePencil from '@lucide/svelte/icons/square-pen';
	import Trash from '@lucide/svelte/icons/trash';
	import Plus from '@lucide/svelte/icons/plus';
	import X from '@lucide/svelte/icons/x';
	import Clock from '@lucide/svelte/icons/clock';
	import ImageIcon from '@lucide/svelte/icons/image';
	import Headphones from '@lucide/svelte/icons/headphones';
	import {
		player,
		isPendingYouTubeTrack,
		queueIdForTrack,
		toQueueItems
	} from '$lib/player/player.svelte';
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
	let editCoverPreview = $state('');
	let editCoverFailed = $state(false);
	let savingEdit = $state(false);

	function openEdit() {
		editCoverPreview = '';
		editCoverFailed = false;
		editing = true;
	}

	function onEditCoverInput(event: Event) {
		const file = (event.currentTarget as HTMLInputElement).files?.[0];
		if (file) editCoverPreview = URL.createObjectURL(file);
	}

	const dateFormatter = new Intl.DateTimeFormat('es', { dateStyle: 'medium' });

	function hueFor(id: string) {
		let hash = 0;
		for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) >>> 0;
		return HUES[hash % HUES.length];
	}

	const isCurrentQueue = $derived(tracks.some((t) => queueIdForTrack(t) === player.current.id));

	function playAll() {
		if (tracks.length === 0) return;
		if (isCurrentQueue) player.toggle();
		else player.playQueue(toQueueItems(tracks), 0);
	}

	function playFrom(index: number) {
		const track = tracks[index];
		if (player.current.id === queueIdForTrack(track)) player.toggle();
		else player.playQueue(toQueueItems(tracks), index);
	}

	function playFromLibrary(index: number) {
		const track = library[index];
		if (player.current.id === queueIdForTrack(track)) player.toggle();
		else player.playQueue(toQueueItems(library), index);
	}
</script>

<svelte:head>
	<title>{playlist.name} · Musify</title>
	<meta name="description" content="Playlist {playlist.name} en Musify." />
</svelte:head>

<section class="px-8 py-8">
	<a
		href="/playlists"
		class="group inline-flex items-center font-medium text-neutral-400 transition-colors hover:text-[var(--mf-accent)]"
	>
		<span
			class="grid h-8 w-8 place-items-center rounded-lg border-white/15 text-neutral-400 transition-colors duration-150 group-hover:text-[var(--mf-accent)]"
		>
			<ArrowLeft class="h-4 w-4" />
		</span>
		Volver a tus listas
	</a>

	<div class="mt-6 flex flex-col gap-6 sm:flex-row sm:items-end">
		<PlaylistArt
			playlistId={playlist.id}
			trackIds={tracks.map((t) => t.id)}
			hue={hueFor(playlist.id)}
			size="large"
			class="h-44 w-44 flex-shrink-0 rounded-2xl shadow-[0_20px_45px_-15px_rgba(0,0,0,0.7)]"
		/>
		<div class="min-w-0 flex-1">
			<p class="text-xs font-semibold uppercase tracking-[0.14em] text-neutral-400">Lista</p>
			<h1 class="mt-1.5 font-display text-5xl font-bold tracking-tight sm:text-7xl">{playlist.name}</h1>
			{#if playlist.description}
				<p class="mt-3 max-w-2xl text-neutral-300">{playlist.description}</p>
			{/if}
			<p class="mt-3 text-sm text-neutral-500">
				{tracks.length}
				{tracks.length === 1 ? 'canción' : 'canciones'}
			</p>
		</div>
	</div>

	<div class="mt-7 flex items-center gap-2.5">
		<button
			type="button"
			onclick={playAll}
			disabled={tracks.length === 0}
			class="inline-flex items-center gap-1.5 rounded-lg bg-[var(--mf-accent)] px-4 py-2 text-neutral-900 transition hover:brightness-110 active:scale-95 disabled:opacity-40"
		>
			{#if isCurrentQueue && player.playing}
				<Pause class="h-4 w-4" fill="currentColor" strokeWidth={1.5} />
				Pausar
			{:else}
				<Play class="h-4 w-4" fill="currentColor" strokeWidth={1.5} />
				Reproducir
			{/if}
		</button>
		<button
			type="button"
			onclick={openEdit}
			class="inline-flex items-center gap-1.5 rounded-lg border border-white/10 px-4 py-2 text-white/65 transition hover:bg-white/2.5"
		>
			<SquarePencil class="h-4 w-4" strokeWidth={1.5} />
			Editar
		</button>
		<button
			type="button"
			onclick={() => (confirmingDelete = true)}
			class="inline-flex items-center gap-1.5 rounded-lg border border-white/10 px-4 py-2 text-white/65 transition hover:bg-white/2.5"
		>
			<Trash class="h-4 w-4" strokeWidth={1.5} />
			Eliminar
		</button>
	</div>

	{#if tracks.length > 0}
		<div class="mt-8">
			<div
				class="text-center grid grid-cols-[32px_1fr_140px_64px_36px] items-center gap-4 border-b border-white/10 px-3 pb-2 text-xs uppercase tracking-wide text-neutral-500"
			>
				<span>#</span>
				<span class="text-left">Título</span>
				<span>Añadida</span>
				<span>Duración</span>
				<span></span>
			</div>
			<div class="mt-1 flex flex-col">
				{#each tracks as track, i (track.id)}
					<div
						class="text-center group grid grid-cols-[32px_1fr_140px_64px_36px] items-center gap-4 rounded-[10px] px-3 py-[9px] transition hover:bg-neutral-900/60"
					>
						<button
							type="button"
							onclick={() => playFrom(i)}
							aria-label={player.current.id === queueIdForTrack(track) && player.playing ? 'Pausar' : 'Reproducir'}
							class="relative grid h-8 w-8 place-items-center overflow-hidden rounded text-sm text-neutral-500"
						>
							{#if player.current.id === queueIdForTrack(track)}
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
							<div class="min-w-0 flex-1">
								<div class="flex min-w-0 items-center gap-2">
									<span class="min-w-0 truncate text-[14.5px] font-semibold text-neutral-100">{track.title}</span>
									{#if track.source === 'YouTube' && track.audioStatus === 'Failed'}
										<span class="shrink-0 rounded-full bg-red-500/10 px-2 py-0.5 text-[10px] font-semibold text-red-300">
											Error
										</span>
									{:else if isPendingYouTubeTrack(track)}
										<span
											class="shrink-0 rounded-full bg-emerald-500/20 px-2 py-0.5 text-[10px] font-semibold text-emerald-400"
										>
											Descargando
										</span>
									{/if}
								</div>
								{#if track.artist}
									<div class="truncate text-left text-xs text-neutral-500">{track.artist}</div>
								{/if}
							</div>
						</button>
						<div class="truncate text-[12.5px] text-neutral-500">
							{dateFormatter.format(new Date(track.createdAt))}
						</div>
						<span class="text-center text-[12.5px] tabular-nums text-neutral-500">
							{fmtTime(Number(track.duration))}
						</span>
						<form method="POST" action="?/removeTrack" use:enhance={() => async ({ update }) => update()}>
							<input type="hidden" name="trackId" value={track.id} />
							<button
								type="submit"
								aria-label="Quitar de la playlist"
								class="grid h-7 w-7 place-items-center rounded-lg text-neutral-500 opacity-0 transition hover:bg-white/5 hover:text-white group-hover:opacity-100"
							>
								<X class="h-3.5 w-3.5" />
							</button>
						</form>
					</div>
				{/each}
			</div>
		</div>
	{/if}

	{#if library.length > 0}
		<div class="mt-12">
			<h2 class="font-display text-lg font-bold tracking-tight">Añadir de tu biblioteca</h2>
			<div
				class="mt-4 grid grid-cols-[1fr_100px_64px_110px] items-center gap-12 border-b border-white/10 px-3 pb-2 text-xs uppercase tracking-wide text-neutral-500"
			>
				<span>Título</span>
				<span class="text-center">Escuchas</span>
				<span class="text-center">Duración</span>
				<span></span>
			</div>
			<div class="mt-1 flex flex-col">
				{#each library as track, i (track.id)}
					<div
						class="group grid grid-cols-[1fr_100px_64px_110px] items-center gap-12 rounded-[10px] px-3 py-[10px] transition hover:bg-neutral-900/60"
					>
						<button
							type="button"
							onclick={() => playFromLibrary(i)}
							class="flex min-w-0 items-center gap-3.5 text-left"
						>
							<Cover
								trackId={track.id}
								hue={hueFor(track.id)}
								size="small"
								alt={track.title}
								class="h-[40px] w-[40px] flex-shrink-0 rounded-md"
							>
								{#if player.current.id === track.id}
									<NowPlaying paused={!player.playing} />
								{/if}
							</Cover>
							<div class="min-w-0 flex-1 truncate text-sm font-medium text-neutral-200">{track.title}</div>
						</button>
						<div class="flex items-center justify-center gap-1.5 text-[12.5px] tabular-nums text-neutral-500">
							<Headphones class="h-3.5 w-3.5" />
							{Number(track.listensCount)}
						</div>
						<span class="text-center text-[12.5px] tabular-nums text-neutral-500">
							{fmtTime(Number(track.duration))}
						</span>
						<form
							method="POST"
							action="?/addTrack"
							use:enhance={() => async ({ update }) => update()}
							class="flex justify-end"
						>
							<input type="hidden" name="trackId" value={track.id} />
							<button
								type="submit"
								class="inline-flex items-center gap-1.5 rounded-lg border-2 border-white/20 px-2 py-2 text-xs text-white/80 transition hover:border-[var(--mf-accent)]/50 hover:text-[var(--mf-accent)]"
							>
								
								<Plus class="h-4 w-4" />
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
		<div class="animate-pop w-full max-w-2xl rounded-2xl bg-[var(--mf-bg)] p-6 shadow-2xl">
			<div class="flex items-center justify-between">
				<h2 class="text-lg font-bold tracking-tight">Editar playlist</h2>
				<button
					type="button"
					onclick={() => (editing = false)}
					aria-label="Cerrar"
					class="grid h-8 w-8 place-items-center rounded-lg text-neutral-400 transition hover:bg-white/5 hover:text-white"
				>
					<X class="h-4 w-4" />
				</button>
			</div>
			<form
				method="POST"
				action="?/rename"
				enctype="multipart/form-data"
				use:enhance={() => {
					savingEdit = true;
					return async ({ update, result }) => {
						await update({ reset: false });
						savingEdit = false;
						if (result.type === 'success') editing = false;
					};
				}}
				class="mt-4 w-full max-w-4xl space-y-4"
			>
				<div class="flex items-stretch gap-8">
					<div class="flex flex-col">
						<span class="mb-3 block text-sm font-medium text-neutral-300">
							Portada <span class="text-neutral-500">(Opcional)</span>
						</span>
						<label
							class="relative grid h-52 w-52 cursor-pointer place-items-center overflow-hidden rounded-lg border border-transparent bg-[var(--mf-surface)] transition-colors hover:border-[var(--mf-accent)]/50"
						>
							<input
								type="file"
								name="cover"
								accept="image/*"
								onchange={onEditCoverInput}
								class="absolute inset-0 cursor-pointer opacity-0"
								aria-label="Seleccionar portada"
							/>
							{#if editCoverPreview !== ''}
								<img src={editCoverPreview} alt="Portada" class="h-full w-full object-cover" />
							{:else if !editCoverFailed}
								<img
									src="/api/playlists/{playlist.id}/cover?size=medium"
									alt="Portada actual"
									loading="lazy"
									onerror={() => (editCoverFailed = true)}
									class="h-full w-full object-cover"
								/>
							{:else}
								<ImageIcon class="h-12 w-12 text-neutral-500" />
							{/if}
						</label>
					</div>

					<div class="flex flex-1 flex-col gap-4">
						<div>
							<label for="edit-name" class="mb-2 block text-sm font-medium text-neutral-300">Nombre</label>
							<input
								id="edit-name"
								name="name"
								type="text"
								maxlength={100}
								required
								value={playlist.name}
								class="w-full rounded-lg border border-transparent bg-[var(--mf-surface)] px-4 py-3 text-sm text-neutral-100 focus:border-[var(--mf-accent)]/50 focus:outline-none"
							/>
						</div>

						<div class="flex min-h-0 flex-1 flex-col">
							<label for="edit-desc" class="mb-2 block text-sm font-medium text-neutral-300">
								Descripción <span class="text-neutral-500">(Opcional)</span>
							</label>
							<textarea
								id="edit-desc"
								name="description"
								rows="3"
								maxlength={300}
								value={playlist.description}
								class="w-full min-h-0 flex-1 resize-none rounded-lg border border-transparent bg-[var(--mf-surface)] px-4 py-3 text-sm text-neutral-100 focus:border-[var(--mf-accent)]/50 focus:outline-none"
							></textarea>
						</div>
					</div>
				</div>

				{#if form?.message}
					<p class="rounded-lg border border-red-500/40 bg-red-500/10 px-3 py-2 text-sm text-red-200">
						{form.message}
					</p>
				{/if}
				<div class="flex items-center justify-end gap-3">
					<button
						type="button"
						onclick={() => (editing = false)}
						class="rounded-lg border border-white/15 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-white/5"
					>
						Cancelar
					</button>
					<button
						type="submit"
						disabled={savingEdit}
						class="rounded-lg bg-[var(--mf-accent)] px-6 py-2.5 text-sm font-semibold text-neutral-800 transition disabled:opacity-40"
					>
						{savingEdit ? 'Guardando…' : 'Guardar'}
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
				Esta acción no se puede deshacer, la playlist se eliminará de tu biblioteca y de todos los dispositivos donde la tengas guardada.
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
