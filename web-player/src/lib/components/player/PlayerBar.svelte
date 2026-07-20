<script lang="ts">
	import TrackInfo from './TrackInfo.svelte';
	import TransportControls from './TransportControls.svelte';
	import SeekBar from './SeekBar.svelte';
	import VolumeControl from './VolumeControl.svelte';
</script>

<div
	class="animate-enter fixed right-0 left-0 z-40 h-[var(--mf-player-h)] border-t border-line bg-bg md:left-[var(--mf-sidebar-w)] md:z-50"
	style="bottom:calc(var(--mf-nav-h) + var(--mf-safe-b))"
>
	<div class="absolute inset-x-0 top-0 md:hidden">
		<SeekBar compact />
	</div>

	<div class="flex h-full items-center gap-3 page-x md:hidden">
		<div class="min-w-0 flex-1">
			<TrackInfo />
		</div>
		<TransportControls compact />
	</div>

	<div
		class="hidden h-full items-center gap-5 px-8 md:grid"
		style="grid-template-columns:minmax(200px,1fr) minmax(0,3fr) minmax(200px,1fr)"
	>
		<TrackInfo />
		<div class="flex flex-col items-center gap-2">
			<TransportControls />
			<SeekBar />
		</div>
		<VolumeControl />
	</div>
</div>
