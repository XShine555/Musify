<script lang="ts">
	import VolumeControl from './VolumeControl.svelte';
	import IconButton from '$lib/components/ui/IconButton.svelte';
	import { queuePanel } from '$lib/player/queuePanel.svelte';
	import ListMusic from '@lucide/svelte/icons/list-music';
</script>

<div class="hidden w-(--mf-player-side-w) shrink-0 items-center justify-end gap-3.5 sm:flex">
	<VolumeControl />
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
