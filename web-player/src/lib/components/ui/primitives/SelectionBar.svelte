<script lang="ts">
	import type { Snippet } from 'svelte';
	import Button from './Button.svelte';

	interface Props {
		label: string;
		onClear: () => void;
		class?: string;
		children: Snippet;
	}

	let { label, onClear, class: klass = '', children }: Props = $props();
</script>

<div class="flex min-h-11 flex-wrap items-center gap-x-4 gap-y-3 {klass}">
	<span class="text-sm text-fg-2">{label}</span>
	{@render children()}
	<Button size="sm" variant="secondary" onclick={onClear}>Deseleccionar</Button>
</div>
