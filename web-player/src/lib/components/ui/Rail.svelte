<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		class?: string;
		children: Snippet;
	}

	let { class: klass = '', children }: Props = $props();

	let track: HTMLDivElement | undefined = $state();

	export function scrollByPage(direction: 1 | -1) {
		track?.scrollBy({ left: direction * track.clientWidth * 0.85, behavior: 'smooth' });
	}
</script>

<div bind:this={track} class="no-scrollbar -mx-1 flex gap-4 overflow-x-auto px-1 pb-2 {klass}">
	{@render children()}
</div>

<style>
	.no-scrollbar {
		scrollbar-width: none;
	}
	.no-scrollbar::-webkit-scrollbar {
		display: none;
	}
</style>
