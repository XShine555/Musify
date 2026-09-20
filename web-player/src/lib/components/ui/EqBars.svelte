<script lang="ts">
	interface Props {
		size?: number;
		barWidth?: number;
		paused?: boolean;
		overlay?: boolean;
		class?: string;
	}

	let {
		size = 13,
		barWidth = 2,
		paused = false,
		overlay = false,
		class: klass = ''
	}: Props = $props();

	const delays = [0, 0.3, 0.6];
</script>

{#if overlay}
	<div class="absolute inset-0 flex items-end justify-center gap-0.75 bg-scrim/45 pb-2.5 {klass}">
		{#each delays as delay (delay)}
			<span
				class="h-3.5 w-0.75 origin-bottom rounded-full bg-on-art"
				style="animation:equalize .9s ease-in-out {delay}s infinite; animation-play-state:{paused
					? 'paused'
					: 'running'}"
			></span>
		{/each}
	</div>
{:else}
	<div class="flex items-end justify-center gap-0.5 {klass}" style="height:{size}px">
		{#each delays as delay (delay)}
			<span
				class="rounded-full bg-accent"
				style="width:{barWidth}px; height:100%; animation:equalize .9s ease-in-out {delay}s infinite; animation-play-state:{paused
					? 'paused'
					: 'running'}"
			></span>
		{/each}
	</div>
{/if}
