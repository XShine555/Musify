<script lang="ts">
	interface Props {
		size?: number;
		barWidth?: number;
		paused?: boolean;
		class?: string;
	}

	let { size = 13, barWidth = 2, paused = false, class: klass = '' }: Props = $props();

	const delays = [0, 0.3, 0.6];
</script>

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
