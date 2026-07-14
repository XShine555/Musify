<script lang="ts">
	import { player } from '$lib/player/player.svelte';
</script>

<div class="flex items-center justify-end gap-3.5">
	<input
		type="range"
		min="0"
		max="100"
		value={player.volume}
		oninput={(event) => player.setVolume(Number(event.currentTarget.value))}
		aria-label="Volumen"
		class="mf-volume"
		style="background:linear-gradient(to right, var(--mf-accent) {player.volume}%, var(--mf-track) {player.volume}%)"
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
