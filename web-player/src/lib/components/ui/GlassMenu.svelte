<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		class?: string;
		style?: string;
		children: Snippet;
	}

	let { class: klass = '', style = '', children }: Props = $props();
</script>

<div class="glass-panel animate-pop rounded-panel p-1.5 shadow-menu {klass}" {style}>
	{@render children()}
</div>
