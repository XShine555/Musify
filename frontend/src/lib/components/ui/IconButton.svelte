<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		label: string;
		type?: 'button' | 'submit';
		size?: 'sm' | 'md';
		revealOnHover?: boolean;
		onclick?: (event: MouseEvent) => void;
		class?: string;
		children: Snippet;
	}

	let {
		label,
		type = 'button',
		size = 'md',
		revealOnHover = false,
		onclick,
		class: klass = '',
		children
	}: Props = $props();

	const sizes = { sm: 'h-7 w-7', md: 'h-8 w-8' };
</script>

<button
	{type}
	{onclick}
	aria-label={label}
	title={label}
	class="grid place-items-center rounded-control text-muted transition hover:bg-hover hover:text-fg {sizes[
		size
	]} {revealOnHover ? 'opacity-0 group-hover:opacity-100 focus-visible:opacity-100' : ''} {klass}"
>
	{@render children()}
</button>
