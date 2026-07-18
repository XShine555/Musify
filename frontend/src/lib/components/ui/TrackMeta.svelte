<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		class?: string;
		children: Snippet;
	}

	let { class: klass = '', children }: Props = $props();
</script>

<span class="truncate text-center text-sm tabular-nums text-muted {klass}">
	{@render children()}
</span>
