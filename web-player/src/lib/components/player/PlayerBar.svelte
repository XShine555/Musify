<script lang="ts">
	import TrackInfo from './TrackInfo.svelte';
	import TransportControls from './TransportControls.svelte';
	import Slider from '$lib/components/ui/primitives/Slider.svelte';
	import IconButton from '$lib/components/ui/primitives/IconButton.svelte';
	import { player } from '$lib/player/player.svelte';
	import { queuePanel } from '$lib/player/queuePanel.svelte';
	import { fmtTime } from '$lib/utils/format';
	import Volume2 from '@lucide/svelte/icons/volume-2';
	import ListMusic from '@lucide/svelte/icons/list-music';

	function seek(value: number) {
		if (player.current.duration > 0) player.seekFraction(value / player.current.duration);
	}
</script>

<div
	class="animate-enter pointer-events-auto flex items-center gap-icon-md glass-bar px-4 py-3 sm:hidden"
>
	<div class="min-w-0 flex-1">
		<TrackInfo compact />
	</div>
	<TransportControls compact />
</div>

<div
	class="animate-enter pointer-events-auto hidden items-center gap-5 glass-bar px-4 py-3 sm:flex"
>
	<div class="w-(--mf-player-side-w) min-w-0 shrink-0">
		<TrackInfo />
	</div>
	<div class="flex min-w-0 flex-1 flex-col items-center gap-2">
		<TransportControls />
		<div class="flex w-full max-w-hero items-center gap-3">
			<span class="min-w-8 text-right text-xs text-fg-2 tabular-nums">
				{fmtTime(player.progress)}
			</span>
			<Slider
				value={player.progress}
				max={player.current.duration}
				label="Barra de progreso"
				oninput={seek}
				variant="seek"
			/>
			<span class="min-w-8 text-xs text-fg-2 tabular-nums">
				{fmtTime(player.current.duration)}
			</span>
		</div>
	</div>
	<div class="hidden w-(--mf-player-side-w) shrink-0 items-center justify-end gap-3.5 sm:flex">
		<Volume2 class="size-icon-sm shrink-0 text-fg-2" strokeWidth={1.8} />
		<Slider
			value={player.volume}
			max={100}
			label="Volumen"
			oninput={(value) => player.setVolume(value)}
			variant="volume"
		/>
		<IconButton
			label="Cola de reproducción"
			tone="plain"
			size="xs"
			pressed={queuePanel.open}
			onclick={() => queuePanel.toggle()}
		>
			<ListMusic class="size-icon-sm" strokeWidth={1.8} />
		</IconButton>
	</div>
</div>
