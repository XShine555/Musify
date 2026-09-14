<script lang="ts">
	import Play from '@lucide/svelte/icons/play';
	import EqBars from './EqBars.svelte';

	interface Props {
		index: number;
		active: boolean;
		playing: boolean;
		onToggle: () => void;
	}

	let { index, active, playing, onToggle }: Props = $props();
</script>

<button
	type="button"
	onclick={onToggle}
	aria-label={active && playing ? 'Pausar' : 'Reproducir'}
	class="relative grid h-8 w-8 place-items-center rounded text-xs text-muted tabular-nums"
>
	{#if active}
		<EqBars paused={!playing} />
	{:else}
		<span class="group-hover:hidden">{index + 1}</span>
		<Play class="hidden h-3.5 w-3.5 text-fg group-hover:block" fill="currentColor" />
	{/if}
</button>
