<script lang="ts">
	import { enhance } from '$app/forms';
	import Plus from '@lucide/svelte/icons/plus';
	import X from '@lucide/svelte/icons/x';
	import ListMusic from '@lucide/svelte/icons/list-music';
	import ImageIcon from '@lucide/svelte/icons/image';
	import { HUES } from '$lib/theme/color';
	import PlaylistArt from '$lib/components/ui/PlaylistArt.svelte';
 
	const playlistNames = [
		"Mi Playlist",
		"Tus favoritos",
		"Lo mejor de hoy",
		"Para relajarse",
		"Buena vibra",
		"Modo estudio",
		"Road Trip",
		"Domingos",
		"Late Night",
		"Descubriendo música",
		"Todo un poco",
		"En bucle",
		"Mi colección",
		"Canciones para repetir",
		"Hits del momento"
	];
	let { data, form } = $props();

	const items = $derived(data.playlists.items);
	const trackIds = $derived(data.trackIds);

	let creating = $state(false);
	let submitting = $state(false);
	let name = $state('');
	let description = $state('');
	let coverPreview = $state('');

	function hueFor(id: string) {
		let hash = 0;
		for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) >>> 0;
		return HUES[hash % HUES.length];
	}

	function openCreate() {
		creating = true;
	}

	function onCoverInput(event: Event) {
		const file = (event.currentTarget as HTMLInputElement).files?.[0];
		if (file) coverPreview = URL.createObjectURL(file);
	}
</script>

<svelte:head>
	<title>Mis listas · Musify</title>
	<meta name="description" content="Mis listas en Musify." />
</svelte:head>

<section class="px-8 py-12">
	<div class="flex items-end justify-between gap-4">
		<div>
			<h1 class="font-display text-3xl font-bold tracking-tight sm:text-4xl">Mis listas</h1>
			<p class="mt-2 text-neutral-400">Crea y organiza tus colecciones.</p>
		</div>
		{#if items.length > 0}
			<button
				type="button"
				onclick={openCreate}
				class="inline-flex items-center gap-2 rounded-lg bg-[var(--mf-accent)] px-5 py-2.5 text-sm font-semibold text-neutral-950 transition hover:brightness-110 hover:shadow-[0_8px_24px_-8px] active:scale-[0.97]"
			>
				<Plus class="h-4 w-4" strokeWidth={2.5} />
				Crear lista
			</button>
		{/if}
	</div>

	{#if items.length > 0}
		<div class="mt-9 grid grid-cols-[repeat(auto-fill,minmax(160px,1fr))] gap-4">
			{#each items as playlist, i (playlist.id)}
				<a
					href="/playlists/{playlist.id}"
					class="group animate-enter text-left"
					style="animation-delay:{i * 45}ms"
				>
					<PlaylistArt
						playlistId={playlist.id}
						trackIds={trackIds[playlist.id] ?? []}
						hue={hueFor(playlist.id)}
						class="aspect-square w-full rounded-xl shadow-[0_12px_28px_-10px_rgba(0,0,0,0.6)] transition group-hover:shadow-[0_16px_36px_-10px_rgba(0,0,0,0.75)]"
					/>
					<div class="mt-2.5 truncate text-sm font-semibold text-[var(--mf-text)]">
						{playlist.name}
					</div>
					{#if playlist.description}
						<div class="mt-0.5 truncate text-xs text-[var(--mf-text-3)]">{playlist.description}</div>
					{/if}
				</a>
			{/each}
		</div>
	{:else}
		<div
			class="mt-9 p-14 text-center"
		>
			<span class="mx-auto grid h-16 w-16 place-items-center rounded-full bg-[var(--mf-accent)] text-neutral-950">
				<ListMusic class="h-7 w-7" />
			</span>
			<h3 class="mt-5 font-display text-2xl font-bold tracking-tight">Todavía no tienes listas</h3>
			<p class="mt-2 text-neutral-400">Crea la primera y añádele canciones de tu biblioteca.</p>
			<button
				type="button"
				onclick={openCreate}
				class="mt-6 inline-flex items-center gap-2 rounded-lg bg-[var(--mf-accent)] px-5 py-2.5 text-sm font-semibold text-neutral-800 transition active:scale-[0.95]"
			>
				<Plus class="h-4 w-4" strokeWidth={2.5} />
				Crear lista
			</button>
		</div>
	{/if}
</section>

{#if creating}
	<div
		class="animate-fade fixed inset-0 z-100 grid place-items-center bg-black/70 p-4 backdrop-blur-sm"
		role="dialog"
		tabindex="0"
		onclick={(e) => {
			if (e.target === e.currentTarget) creating = false;
		}}
		onkeydown={(e) => {
			if (e.key === 'Escape') creating = false;
		}}
	>
		<div class="animate-pop w-full max-w-2xl rounded-lg bg-[var(--mf-bg)] p-6 shadow-2xl">
			<div class="flex items-center justify-between">
				<h2 class="text-2xl font-bold tracking-tight">Nueva lista</h2>
				<button
					type="button"
					onclick={() => (creating = false)}
					aria-label="Cerrar"
					class="grid h-8 w-8 place-items-center rounded-lg text-neutral-400 transition hover:bg-white/5 hover:text-white"
				>
					<X class="h-4 w-4" />
				</button>
			</div>

			<form
				method="POST"
				action="?/create"
				enctype="multipart/form-data"
				use:enhance={() => {
					submitting = true;
					return async ({ update }) => {
						await update();
						submitting = false;
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
							class="relative grid h-52 w-52 cursor-pointer place-items-center overflow-hidden rounded-lg border border-transparent bg-[var(--mf-surface)] transition-colors hover:border-[var(--mf-accent)]/50 "
						>
							<input
								type="file"
								name="cover"
								accept="image/*"
								onchange={onCoverInput}
								class="absolute inset-0 cursor-pointer opacity-0"
								aria-label="Seleccionar portada"
							/>
							{#if coverPreview !== ''}
								<img src={coverPreview} alt="Portada" class="h-full w-full object-cover" />
							{:else}
								<ImageIcon class="h-12 w-12 text-neutral-500" />
							{/if}
						</label>
					</div>

					<div class="flex flex-1 flex-col gap-4">
						<div>
							<label for="name" class="mb-2 block text-sm font-medium text-neutral-300">Nombre</label>
							<input
								id="name"
								name="name"
								type="text"
								maxlength={100}
								required
								autocomplete="off"
								bind:value={name}
								placeholder={playlistNames[Math.floor(Math.random() * playlistNames.length)]}
								class="w-full rounded-lg border border-transparent bg-[var(--mf-surface)] px-4 py-3 text-sm text-neutral-100 placeholder:text-neutral-500 focus:border-[var(--mf-accent)]/50 focus:outline-none"
							/>
						</div>

						<div class="flex min-h-0 flex-1 flex-col">
							<label for="description" class="mb-2 block text-sm font-medium text-neutral-300">
								Descripción <span class="text-neutral-500">(Opcional)</span>
							</label>
							<textarea
								id="description"
								name="description"
								maxlength={300}
								bind:value={description}
								placeholder="¿De qué va esta lista?"
								class="w-full min-h-0 flex-1 resize-none rounded-lg border border-transparent bg-[var(--mf-surface)] px-4 py-3 text-sm text-neutral-100 placeholder:text-neutral-500 focus:border-[var(--mf-accent)]/50 focus:outline-none"
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
						onclick={() => (creating = false)}
						class="rounded-lg border border-white/10 px-5 py-2.5 text-sm text-white/65 transition hover:bg-white/2.5"
					>
						Cancelar
					</button>
					<button
						type="submit"
						disabled={submitting || name.trim() === ''}
						class="rounded-lg bg-[var(--mf-accent)] px-6 py-2.5 text-sm font-semibold text-neutral-800 transition disabled:opacity-40"
					>
						{submitting ? 'Creando…' : 'Crear'}
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}
