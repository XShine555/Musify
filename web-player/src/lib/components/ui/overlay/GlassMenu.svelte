<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		class?: string;
		style?: string;
		children: Snippet;
	}

	let { class: klass = '', style = '', children }: Props = $props();
</script>

<div class="glass-panel animate-pop rounded-panel border border-hairline p-1.5 {klass}" {style}>
	{@render children()}
</div>
