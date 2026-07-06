<script lang="ts">
	import { player } from '$lib/player/player.svelte';

	function format(seconds: number) {
		if (!Number.isFinite(seconds) || seconds < 0) return '0:00';
		const minutes = Math.floor(seconds / 60);
		const rest = Math.floor(seconds % 60);
		return `${minutes}:${rest.toString().padStart(2, '0')}`;
	}
</script>

<div class="flex w-full items-center gap-2">
	<span class="w-10 text-right text-xs tabular-nums text-neutral-500">{format(player.currentTime)}</span>
	<input
		type="range"
		min="0"
		max={player.duration || 0}
		step="0.1"
		value={player.currentTime}
		disabled={!player.duration}
		oninput={(event) => player.seek(Number(event.currentTarget.value))}
		aria-label="Progreso"
		class="h-1 w-full cursor-pointer accent-emerald-500"
	/>
	<span class="w-10 text-xs tabular-nums text-neutral-500">{format(player.duration)}</span>
</div>
