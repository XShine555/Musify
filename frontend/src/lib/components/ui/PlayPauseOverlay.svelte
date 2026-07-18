<script lang="ts">
	import Play from '@lucide/svelte/icons/play';
	import Pause from '@lucide/svelte/icons/pause';

	interface Props {
		active: boolean;
		playing: boolean;
	}

	let { active, playing }: Props = $props();
</script>

<span
	class="absolute right-2.5 bottom-2.5 grid h-11 w-11 translate-y-2 place-items-center rounded-full bg-accent-soft text-on-accent opacity-0 shadow-lg transition-all group-hover:translate-y-0 group-hover:opacity-100"
	class:!opacity-100={active}
	class:!translate-y-0={active}
>
	{#if active && playing}
		<Pause class="h-5 w-5" fill="currentColor" />
	{:else}
		<Play class="h-5 w-5" fill="currentColor" />
	{/if}
</span>
