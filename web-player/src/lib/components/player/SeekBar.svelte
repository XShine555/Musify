<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import { fmtTime } from '$lib/format';

	interface Props {
		compact?: boolean;
	}

	let { compact = false }: Props = $props();

	function seek(event: MouseEvent) {
		const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
		player.seekFraction((event.clientX - rect.left) / rect.width);
	}
</script>

{#if compact}
	<button
		type="button"
		onclick={seek}
		aria-label="Barra de progreso"
		class="relative block h-1 w-full cursor-pointer bg-track"
	>
		<div
			class="absolute inset-y-0 left-0 bg-accent/70"
			style="width:{player.progressPercent}%"
		></div>
	</button>
{:else}
	<div class="flex w-full max-w-135 items-center gap-2.75">
		<span class="min-w-8 text-right text-xs text-fg-3 tabular-nums">
			{fmtTime(player.progress)}
		</span>
		<button
			type="button"
			onclick={seek}
			aria-label="Barra de progreso"
			class="group relative h-3.5 flex-1 cursor-pointer"
		>
			<div class="h-[3.5px] w-full overflow-hidden rounded-full bg-track">
				<div class="h-full rounded-full bg-accent" style="width:{player.progressPercent}%"></div>
			</div>
		</button>
		<span class="min-w-8 text-xs text-fg-3 tabular-nums">
			{fmtTime(player.current.duration)}
		</span>
	</div>
{/if}
