<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import { fmtTime } from '$lib/theme/color';

	function seek(event: MouseEvent) {
		const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
		player.seekFraction((event.clientX - rect.left) / rect.width);
	}
</script>

<div class="flex w-full max-w-172 items-center gap-3">
	<span class="text-right text-xs tabular-nums text-muted">
		{fmtTime(player.progress)}
	</span>
	<button
		type="button"
		onclick={seek}
		aria-label="Barra de progreso"
		class="group relative h-1.5 flex-1 cursor-pointer rounded-full bg-track transition-[height] hover:h-2"
	>
		<div
			class="absolute inset-y-0 left-0 rounded-full bg-accent/60 group-hover:brightness-110"
			style="width:{player.progressPercent}%"
		></div>
		<span
			class="pointer-events-none absolute top-1/2 h-3 w-3 -translate-x-1/2 -translate-y-1/2 rounded-full bg-white opacity-0 shadow-[0_1px_4px_rgba(0,0,0,0.5)] transition-opacity duration-75 group-hover:opacity-100"
			style="left:{player.progressPercent}%"
		></span>
	</button>
	<span class="text-xs tabular-nums text-(--mf-text-4)">
		{fmtTime(player.current.duration)}
	</span>
</div>
