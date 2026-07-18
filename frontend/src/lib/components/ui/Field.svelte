<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		label: string;
		for?: string;
		optional?: boolean;
		class?: string;
		hint?: Snippet;
		children: Snippet;
	}

	let { label, for: forId, optional = false, class: klass = '', hint, children }: Props = $props();
</script>

<div class="flex min-w-0 flex-col {klass}">
	<label for={forId} class="mb-2 block text-base font-medium text-fg-2">
		{label}
		{#if optional}<span class="text-muted">(Opcional)</span>{/if}
	</label>
	{@render children()}
	{#if hint}
		<div class="mt-2 flex items-center justify-between gap-3 text-sm text-muted">
			{@render hint()}
		</div>
	{/if}
</div>
