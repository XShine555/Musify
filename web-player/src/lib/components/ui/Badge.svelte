<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		tone?: 'neutral' | 'accent' | 'danger';
		class?: string;
		children: Snippet;
	}

	let { tone = 'neutral', class: klass = '', children }: Props = $props();

	const tones = {
		neutral: 'bg-surface-2 text-fg',
		accent: 'bg-accent/20 text-accent',
		danger: 'bg-danger/10 text-danger-fg'
	};
</script>

<span
	class="shrink-0 rounded-full px-2 py-0.5 text-sm leading-none font-semibold {tones[tone]} {klass}"
>
	{@render children()}
</span>
