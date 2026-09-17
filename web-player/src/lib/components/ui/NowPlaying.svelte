<script lang="ts">
	interface Props {
		paused?: boolean;
	}

	let { paused = false }: Props = $props();

	const delays = [0, 0.3, 0.6];
</script>

<div class="absolute inset-0 flex items-end justify-center gap-0.75 bg-scrim/45 pb-2.5">
	{#each delays as delay (delay)}
		<span
			class="h-3.5 w-0.75 origin-bottom rounded-full bg-on-art"
			style="animation:equalize .9s ease-in-out {delay}s infinite; animation-play-state:{paused
				? 'paused'
				: 'running'}"
		></span>
	{/each}
</div>
