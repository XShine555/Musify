<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		label: string;
		type?: 'button' | 'submit';
		size?: 'xs' | 'sm' | 'md' | 'lg';
		shape?: 'square' | 'round';
		plain?: boolean;
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
		plain = false,
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

	const background = $derived(
		surface ? 'bg-surface-2 hover:bg-surface-hover' : plain ? '' : 'hover:bg-hover'
	);

	const classes = $derived(
		`grid place-items-center transition focus-ring ${SIZE[size]} ${shape === 'round' ? 'rounded-full' : 'rounded-control'} ${pressed ? 'text-accent' : 'text-fg-2 hover:text-fg'} ${background} ${
			revealOnHover ? 'sm:opacity-0 sm:group-hover:opacity-100 sm:focus-visible:opacity-100' : ''
		} ${klass}`
	);
</script>

<!-- svelte-ignore a11y_no_static_element_interactions -->
<svelte:element
	this={href ? 'a' : 'button'}
	{href}
	type={href ? undefined : type}
	data-sveltekit-reload={href && reload ? '' : undefined}
	aria-label={label}
	aria-pressed={href ? undefined : pressed}
	title={label}
	{onclick}
	class={classes}
>
	{@render children()}
</svelte:element>
