<script lang="ts">
	import type { Snippet } from 'svelte';
	import LoaderCircle from '@lucide/svelte/icons/loader-circle';

	interface Props {
		variant?: 'primary' | 'secondary' | 'accent' | 'danger';
		size?: 'sm' | 'md' | 'lg';
		type?: 'button' | 'submit';
		href?: string;
		reload?: boolean;
		disabled?: boolean;
		loading?: boolean;
		onclick?: (event: MouseEvent) => void;
		class?: string;
		children: Snippet;
	}

	let {
		variant = 'primary',
		size = 'md',
		type = 'button',
		href,
		reload = false,
		disabled = false,
		loading = false,
		onclick,
		class: klass = '',
		children
	}: Props = $props();

	const SIZE = {
		sm: 'rounded-control px-4 py-2 text-xs',
		md: 'rounded-btn-sm px-4.5 py-2.5 text-control-sm',
		lg: 'rounded-btn px-6 py-3.5 text-sm'
	};

	const VARIANT = {
		primary: 'bg-cta-strong text-ink hover:bg-cta-strong-hover',
		secondary: 'bg-btn text-fg hover:bg-btn-hover',
		accent: 'surface-active text-accent-soft hover:surface-active-hover',
		danger: 'bg-danger text-on-art hover:brightness-110'
	};

	const classes = $derived(
		`inline-flex items-center justify-center gap-2 font-medium btn-transition focus-ring focus-visible:ring-offset-2 focus-visible:ring-offset-bg disabled:opacity-40 ${SIZE[size]} ${VARIANT[variant]} ${klass}`
	);
</script>

<!-- svelte-ignore a11y_no_static_element_interactions -->
<svelte:element
	this={href ? 'a' : 'button'}
	{href}
	type={href ? undefined : type}
	disabled={href ? undefined : disabled || loading}
	data-sveltekit-reload={href && reload ? '' : undefined}
	{onclick}
	class={classes}
>
	{#if loading && !href}
		<LoaderCircle class="size-icon-sm animate-spin" />
	{/if}
	{@render children()}
</svelte:element>
