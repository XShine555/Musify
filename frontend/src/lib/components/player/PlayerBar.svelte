<script lang="ts">
	import { player } from '$lib/player/player.svelte';
	import TrackInfo from './TrackInfo.svelte';
	import TransportControls from './TransportControls.svelte';
	import SeekBar from './SeekBar.svelte';
	import VolumeControl from './VolumeControl.svelte';
</script>

{#if player.current}
	<div class="fixed inset-x-0 bottom-0 z-50 border-t border-white/10 bg-neutral-950/95 backdrop-blur">
		<div class="mx-auto grid max-w-6xl grid-cols-[1fr_auto] items-center gap-4 px-4 py-3 sm:grid-cols-3 sm:px-6">
			<TrackInfo />

			<div class="flex flex-col items-center gap-2">
				<TransportControls />
				<div class="hidden w-full max-w-md sm:block">
					<SeekBar />
				</div>
			</div>

			<div class="hidden justify-end sm:flex">
				<VolumeControl />
			</div>
		</div>
	</div>
{/if}
