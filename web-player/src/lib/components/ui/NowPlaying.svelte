<script lang="ts">
	interface Props {
		paused?: boolean;
	}

	let { paused = false }: Props = $props();

	const bars = [
		{ delay: 0, dur: 0.9 },
		{ delay: 0.25, dur: 0.7 },
		{ delay: 0.1, dur: 1.05 },
		{ delay: 0.4, dur: 0.8 }
	];
</script>

<div class="absolute inset-0 flex items-end justify-center gap-[2.5px] bg-scrim/45 pb-2">
	{#each bars as bar, i (i)}
		<span
			class="w-[3px] origin-bottom rounded-full bg-on-art"
			style="height:14px; animation:equalize {bar.dur}s ease-in-out {bar.delay}s infinite; animation-play-state:{paused
				? 'paused'
				: 'running'}"
		></span>
	{/each}
</div>
