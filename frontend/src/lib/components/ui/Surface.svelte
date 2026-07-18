<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		padding?: 'sm' | 'md' | 'lg';
		class?: string;
		children: Snippet;
	}

	let { padding = 'md', class: klass = '', children }: Props = $props();

	const paddings = { sm: 'p-4', md: 'p-6', lg: 'p-12' };
</script>

<div class="rounded-panel border border-line bg-surface transition {paddings[padding]} {klass}">
	{@render children()}
</div>
