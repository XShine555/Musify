<script lang="ts">
	import TrackInfo from './TrackInfo.svelte';
	import TransportControls from './TransportControls.svelte';
	import SeekBar from './SeekBar.svelte';
	import PlayerExtras from './PlayerExtras.svelte';
</script>

<div
	class="animate-enter pointer-events-auto flex items-center gap-4.5 rounded-[18px] border border-hairline bg-[image:var(--mf-bar-bg)] px-4 py-2.75 shadow-player backdrop-blur-[22px] transition-[background] duration-[600ms] sm:hidden"
>
	<div class="min-w-0 flex-1">
		<TrackInfo compact />
	</div>
	<TransportControls compact />
</div>

<div
	class="animate-enter pointer-events-auto hidden items-center gap-5 rounded-[18px] border border-hairline bg-[image:var(--mf-bar-bg)] px-4 py-2.75 shadow-player backdrop-blur-[22px] transition-[background] duration-[600ms] sm:flex"
>
	<div class="w-64 min-w-0 shrink-0">
		<TrackInfo />
	</div>
	<div class="flex min-w-0 flex-1 flex-col items-center gap-2">
		<TransportControls />
		<SeekBar />
	</div>
	<PlayerExtras />
</div>
