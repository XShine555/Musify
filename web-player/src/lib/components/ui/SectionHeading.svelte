<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		title: string;
		class?: string;
		actions?: Snippet;
	}

	let { title, class: klass = '', actions }: Props = $props();
</script>

{#if actions}
	<div class="mb-4 flex items-end justify-between gap-4 sm:mb-5 {klass}">
		<h2 class="font-display text-xl font-medium tracking-tight text-fg sm:text-[1.75rem]">
			{title}
		</h2>
		{@render actions()}
	</div>
{:else}
	<h2
		class="mb-4 font-display text-xl font-medium tracking-tight text-fg sm:mb-5 sm:text-[1.75rem] {klass}"
	>
		{title}
	</h2>
{/if}
