<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		tone?: 'neutral' | 'danger';
		class?: string;
		children: Snippet;
	}

	let { tone = 'neutral', class: klass = '', children }: Props = $props();

	const tones = {
		neutral: 'border-line bg-surface text-fg-2',
		danger: 'border-danger/20 bg-danger/10 text-danger-fg'
	};
</script>

<p class="rounded-control border px-4 py-3 text-sm backdrop-blur-md {tones[tone]} {klass}">
	{@render children()}
</p>
