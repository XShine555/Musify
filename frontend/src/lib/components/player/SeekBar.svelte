<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import { fmtTime } from '$lib/theme/color';

	function seek(event: MouseEvent) {
		const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
		player.seekFraction((event.clientX - rect.left) / rect.width);
	}
</script>

<div class="flex w-full max-w-[900px] items-center gap-[9px]">
	<span class="w-8 text-right text-[11px] tabular-nums text-[var(--mf-text-4)]">
		{fmtTime(player.progress)}
	</span>
	<button
		type="button"
		onclick={seek}
		aria-label="Barra de progreso"
		class="group relative h-1.5 flex-1 cursor-pointer rounded-full bg-[var(--mf-track)] transition-[height] hover:h-2"
	>
		<div
			class="absolute inset-y-0 left-0 rounded-full bg-[var(--mf-accent)] transition-[width] duration-75 ease-linear group-hover:brightness-110"
			style="width:{player.progressPercent}%"
		></div>
		<span
			class="pointer-events-none absolute top-1/2 h-3 w-3 -translate-x-1/2 -translate-y-1/2 rounded-full bg-white opacity-0 shadow-[0_1px_4px_rgba(0,0,0,0.5)] transition-[left,opacity] duration-75 ease-linear group-hover:opacity-100"
			style="left:{player.progressPercent}%"
		></span>
	</button>
	<span class="w-8 text-[11px] tabular-nums text-[var(--mf-text-4)]">
		{fmtTime(player.current.duration)}
	</span>
</div>
