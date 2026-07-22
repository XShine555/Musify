<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		class?: string;
		children: Snippet;
	}

	let { class: klass = '', children }: Props = $props();
</script>

<div class="no-scrollbar -mx-1 flex gap-4 overflow-x-auto px-1 pb-2 {klass}">
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
