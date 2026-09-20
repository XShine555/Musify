<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		label: string;
		type?: 'button' | 'submit';
		size?: 'xs' | 'sm' | 'md' | 'lg';
		shape?: 'square' | 'round';
		tone?: 'muted' | 'subtle' | 'plain';
		pressed?: boolean;
		surface?: boolean;
		revealOnHover?: boolean;
		href?: string;
		reload?: boolean;
		onclick?: (event: MouseEvent) => void;
		class?: string;
		children: Snippet;
	}

	let {
		label,
		type = 'button',
		size = 'sm',
		shape = 'square',
		tone = 'muted',
		pressed,
		surface = false,
		revealOnHover = false,
		href,
		reload = false,
		onclick,
		class: klass = '',
		children
	}: Props = $props();

	const SIZE: Record<'xs' | 'sm' | 'md' | 'lg', string> = {
		xs: 'size-7',
		sm: 'size-8',
		md: 'size-9',
		lg: 'size-10'
	};

	const TEXT: Record<'muted' | 'subtle' | 'plain', string> = {
		muted: 'text-muted hover:text-fg',
		subtle: 'text-fg-2 hover:text-fg',
		plain: 'text-fg-2 hover:text-fg'
	};

	const background = $derived(
		surface ? 'bg-surface-2 hover:bg-surface-hover' : tone === 'plain' ? '' : 'hover:bg-hover'
	);

	const classes = $derived(
		`grid place-items-center transition focus-visible:ring-2 focus-visible:ring-accent focus-visible:outline-none ${SIZE[size]} ${shape === 'round' ? 'rounded-full' : 'rounded-control'} ${pressed ? 'text-accent' : TEXT[tone]} ${background} ${
			revealOnHover ? 'sm:opacity-0 sm:group-hover:opacity-100 sm:focus-visible:opacity-100' : ''
		} ${klass}`
	);
</script>

{#if href}
	<a
		{href}
		data-sveltekit-reload={reload ? '' : undefined}
		aria-label={label}
		title={label}
		class={classes}
	>
		{@render children()}
	</a>
{:else}
	<button {type} {onclick} aria-label={label} title={label} aria-pressed={pressed} class={classes}>
		{@render children()}
	</button>
{/if}
