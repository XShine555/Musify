<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import Volume2 from '@lucide/svelte/icons/volume-2';

	function handleVolumeChange(event: Event) {
		const target = event.currentTarget as HTMLInputElement;
		player.setVolume(Number(target.value));
	}
</script>

<div class="flex items-center gap-2.25">
	<Volume2 class="h-3.75 w-3.75 shrink-0 text-fg-3" strokeWidth={1.8} />
	<input
		type="range"
		min="0"
		max="100"
		value={player.volume}
		oninput={handleVolumeChange}
		aria-label="Volumen"
		class="mf-volume"
		style="background:linear-gradient(to right, var(--mf-text-2) {player.volume}%, var(--mf-track) {player.volume}%)"
	/>
</div>

<style>
	.mf-volume {
		-webkit-appearance: none;
		appearance: none;
		width: 84px;
		height: 3.5px;
		border-radius: 100px;
		outline: none;
		cursor: pointer;
	}
	.mf-volume::-webkit-slider-thumb {
		-webkit-appearance: none;
		appearance: none;
		width: 11px;
		height: 11px;
		border-radius: 50%;
		background: var(--mf-text);
		cursor: pointer;
	}
	.mf-volume::-moz-range-thumb {
		width: 11px;
		height: 11px;
		border-radius: 50%;
		background: var(--mf-text);
		border: none;
		cursor: pointer;
	}
</style>
