<script lang="ts">
	import { player } from '$lib/player/player.svelte';

	function handleVolumeChange(event: Event) {
		const target = event.currentTarget as HTMLInputElement;
		const newVolume = Number(target.value);
		player.setVolume(newVolume);
	}
</script>

<div class="flex items-center justify-end gap-3.5">
	<input
		type="range"
		min="0"
		max="100"
		value={player.volume}
		oninput={handleVolumeChange}
		aria-label="Volumen"
		class="mf-volume"
		style="background:linear-gradient(to right, var(--mf-accent-60) {player.volume}%, var(--mf-track) {player.volume}%)"
	/>
</div>

<style>
	.mf-volume {
		-webkit-appearance: none;
		appearance: none;
		width: 80px;
		height: 4px;
		border-radius: 100px;
		outline: none;
		cursor: pointer;
	}
	.mf-volume::-webkit-slider-thumb {
		-webkit-appearance: none;
		appearance: none;
		width: 12px;
		height: 12px;
		border-radius: 50%;
		background: var(--mf-text);
		cursor: pointer;
	}
	.mf-volume::-moz-range-thumb {
		width: 12px;
		height: 12px;
		border-radius: 50%;
		background: var(--mf-text);
		border: none;
		cursor: pointer;
	}
</style>
