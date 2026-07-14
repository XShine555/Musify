<script lang="ts">
	import { player } from '$lib/player/player.svelte';
</script>

<div class="flex items-center gap-5">
	<button
		type="button"
		onclick={() => player.previous()}
		aria-label="Anterior"
		class="flex text-[var(--mf-text)] opacity-70 transition hover:opacity-100 active:scale-90"
	>
		<svg width="16" height="14" viewBox="0 0 16 14" fill="currentColor">
			<rect x="0" y="0" width="2.5" height="14" rx="1" />
			<path d="M15 0 5 7l10 7z" />
		</svg>
	</button>

	<button
		type="button"
		onclick={() => player.toggle()}
		aria-label={player.playing ? 'Pausar' : 'Reproducir'}
		class="grid h-[36px] w-[36px] place-items-center rounded-full bg-[var(--mf-text)] text-[var(--mf-bg)] shadow-lg transition duration-100 active:scale-95"
	>
		{#if player.playing}
			<svg width="12" height="12" viewBox="0 0 12 12" fill="currentColor">
				<rect x="1.5" y="0" width="3.5" height="12" rx="1" />
				<rect x="7" y="0" width="3.5" height="12" rx="1" />
			</svg>
		{:else}
			<svg width="12" height="12" viewBox="0 0 12 12" fill="currentColor">
				<path d="M2.5 0 12 6l-9.5 6z" />
			</svg>
		{/if}
	</button>

	<button
		type="button"
		onclick={() => player.next()}
		aria-label="Siguiente"
		class="flex text-[var(--mf-text)] opacity-70 transition hover:opacity-100 active:scale-90"
	>
		<svg width="16" height="14" viewBox="0 0 16 14" fill="currentColor">
			<path d="M1 0 11 7 1 14z" />
			<rect x="13.5" y="0" width="2.5" height="14" rx="1" />
		</svg>
	</button>
</div>
