<script lang="ts">
	interface Props {
		paused?: boolean;
	}

	let { paused = false }: Props = $props();

	const delays = [0, 0.3, 0.6];
</script>

<div class="absolute inset-0 flex items-end justify-center gap-[3px] bg-scrim/45 pb-2.5">
	{#each delays as delay (delay)}
		<span
			class="w-[3px] origin-bottom rounded-full bg-on-art"
			style="height:14px; animation:equalize .9s ease-in-out {delay}s infinite; animation-play-state:{paused
				? 'paused'
				: 'running'}"
		></span>
	{/each}
</div>
