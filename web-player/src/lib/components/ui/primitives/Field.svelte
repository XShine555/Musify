<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		label?: string;
		for?: string;
		class?: string;
		hint?: Snippet;
		children: Snippet;
	}

	let { label, for: forId, class: klass = '', hint, children }: Props = $props();
</script>

<div class="flex min-w-0 flex-col {klass}">
	{#if label}
		<label for={forId} class="mb-2 block text-sm font-medium text-fg-2">
			{label}
		</label>
	{/if}
	{@render children()}
	{#if hint}
		<div class="mt-2 flex items-center justify-between gap-3 text-xs text-muted">
			{@render hint()}
		</div>
	{/if}
</div>
