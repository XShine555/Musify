<script lang="ts">
	import { player } from '$lib/player/player.svelte';
</script>

<div class="flex items-center gap-2">
	<button
		type="button"
		onclick={() => player.previous()}
		disabled={!player.current}
		aria-label="Anterior"
		class="grid h-9 w-9 place-items-center rounded-full text-neutral-300 transition hover:text-white disabled:opacity-30"
	>
		<svg viewBox="0 0 24 24" fill="currentColor" class="h-5 w-5">
			<path d="M7 6h2v12H7z" />
			<path d="M20 6v12l-9-6z" />
		</svg>
	</button>

	<button
		type="button"
		onclick={() => player.toggle()}
		disabled={!player.current}
		aria-label={player.isPlaying ? 'Pausar' : 'Reproducir'}
		class="grid h-11 w-11 place-items-center rounded-full bg-emerald-500 text-neutral-950 transition hover:bg-emerald-400 disabled:opacity-40"
	>
		{#if player.loading}
			<svg class="h-5 w-5 animate-spin" viewBox="0 0 24 24" fill="none">
				<circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
				<path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" stroke-width="3" stroke-linecap="round" />
			</svg>
		{:else if player.isPlaying}
			<svg viewBox="0 0 24 24" fill="currentColor" class="h-5 w-5">
				<path d="M6 5h4v14H6zM14 5h4v14h-4z" />
			</svg>
		{:else}
			<svg viewBox="0 0 24 24" fill="currentColor" class="h-5 w-5">
				<path d="M8 5v14l11-7z" />
			</svg>
		{/if}
	</button>

	<button
		type="button"
		onclick={() => player.next()}
		disabled={!player.hasNext}
		aria-label="Siguiente"
		class="grid h-9 w-9 place-items-center rounded-full text-neutral-300 transition hover:text-white disabled:opacity-30"
	>
		<svg viewBox="0 0 24 24" fill="currentColor" class="h-5 w-5">
			<path d="M15 6h2v12h-2z" />
			<path d="M4 6v12l9-6z" />
		</svg>
	</button>
</div>
