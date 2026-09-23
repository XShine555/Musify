<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		class?: string;
		style?: string;
		border?: boolean;
		children: Snippet;
	}

	let { class: klass = '', style = '', border = true, children }: Props = $props();
</script>

<div
	class="glass-panel animate-pop rounded-panel p-1.5 {border
		? 'border border-hairline'
		: ''} {klass}"
	{style}
>
	{@render children()}
</div>
