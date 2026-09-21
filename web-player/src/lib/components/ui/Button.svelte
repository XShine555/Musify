<script lang="ts">
	import type { Snippet } from 'svelte';
	import LoaderCircle from '@lucide/svelte/icons/loader-circle';

	interface Props {
		variant?: 'primary' | 'secondary' | 'glass' | 'accent' | 'strong' | 'danger';
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

	const sizes = {
		sm: 'px-4 py-2.5 text-xs',
		md: 'px-5 py-3 text-sm',
		lg: 'px-6 py-3.5 text-sm'
	};

	const subtle = 'bg-btn text-fg-2 hover:bg-btn-hover hover:text-fg';
	const glass = 'font-normal bg-btn-glass text-fg-2 hover:bg-btn-glass-hover hover:text-fg';

	const variants = {
		primary: subtle,
		secondary: subtle,
		glass,
		accent: 'bg-accent-btn text-accent-soft hover:bg-accent-btn-hover',
		strong: 'bg-cta-strong text-ink hover:brightness-95',
		danger: 'bg-danger text-on-art hover:brightness-110'
	};

	const classes = $derived(
		`inline-flex items-center justify-center gap-2 rounded-control transition focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg focus-visible:outline-none disabled:opacity-40 ${sizes[size]} ${variants[variant]} ${klass}`
	);
</script>

{#if href}
	<a {href} data-sveltekit-reload={reload ? '' : undefined} class={classes}>
		{@render children()}
	</a>
{:else}
	<button {type} disabled={disabled || loading} {onclick} class={classes}>
		{#if loading}
			<LoaderCircle class="size-4 animate-spin" />
		{/if}
		{@render children()}
	</button>
{/if}
