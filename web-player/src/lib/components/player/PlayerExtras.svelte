<script lang="ts">
	import VolumeControl from './VolumeControl.svelte';
	import { queuePanel } from '$lib/player/queuePanel.svelte';
	import AlignJustify from '@lucide/svelte/icons/align-justify';
	import ListMusic from '@lucide/svelte/icons/list-music';
</script>

<div class="hidden w-64 shrink-0 items-center justify-end gap-3.5 sm:flex">
	<span aria-hidden="true" class="text-fg-3">
		<AlignJustify class="h-[15px] w-[15px]" strokeWidth={1.8} />
	</span>
	<VolumeControl />
	<button
		type="button"
		onclick={() => queuePanel.toggle()}
		aria-pressed={queuePanel.open}
		aria-label="Cola de reproducción"
		class="transition-colors hover:text-fg {queuePanel.open ? 'text-fg' : 'text-fg-2'}"
	>
		<ListMusic class="h-4 w-4" strokeWidth={1.8} />
	</button>
</div>
