<script lang="ts">
	import { player } from '$lib/player/player.svelte';
</script>

<div class="flex items-center gap-2">
	<button
		type="button"
		onclick={() => player.toggleMute()}
		aria-label={player.muted ? 'Activar sonido' : 'Silenciar'}
		class="grid h-9 w-9 place-items-center rounded-full text-neutral-300 transition hover:text-white"
	>
		{#if player.muted || player.volume === 0}
			<svg viewBox="0 0 24 24" fill="currentColor" class="h-5 w-5">
				<path d="M4 9v6h4l5 5V4L8 9H4z" />
				<path d="M16 9l5 6M21 9l-5 6" stroke="currentColor" stroke-width="2" fill="none" />
			</svg>
		{:else}
			<svg viewBox="0 0 24 24" fill="currentColor" class="h-5 w-5">
				<path d="M4 9v6h4l5 5V4L8 9H4z" />
				<path d="M16 8a5 5 0 0 1 0 8" stroke="currentColor" stroke-width="2" fill="none" />
			</svg>
		{/if}
	</button>
	<input
		type="range"
		min="0"
		max="1"
		step="0.01"
		value={player.muted ? 0 : player.volume}
		oninput={(event) => player.setVolume(Number(event.currentTarget.value))}
		aria-label="Volumen"
		class="h-1 w-24 cursor-pointer accent-emerald-500"
	/>
</div>
