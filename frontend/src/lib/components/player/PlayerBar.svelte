<script lang="ts">
	import TrackInfo from './TrackInfo.svelte';
	import TransportControls from './TransportControls.svelte';
	import SeekBar from './SeekBar.svelte';
	import VolumeControl from './VolumeControl.svelte';
</script>

<div
	class="animate-enter fixed bottom-0 left-[264px] right-0 z-50 h-[92px] border-t border-[var(--mf-border)] bg-[var(--mf-bg)]"
>
	<div
		class="grid h-full items-center gap-5 px-8"
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
