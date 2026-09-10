<script lang="ts">
	import type { Snippet } from 'svelte';
	import LoaderCircle from '@lucide/svelte/icons/loader-circle';

	interface Props {
		variant?: 'primary' | 'secondary' | 'subtle' | 'danger';
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
		sm: 'px-4 py-2 text-base',
		md: 'px-5 py-2.5 text-base',
		lg: 'px-8 py-3.5 text-base'
	};

	const variants = {
		primary: 'bg-accent-soft text-on-accent active:scale-[0.97]',
		secondary: 'bg-surface-2 text-fg hover:bg-accent/12',
		subtle: 'bg-white/10 text-fg hover:bg-accent/18',
		danger: 'bg-danger text-on-accent hover:brightness-110'
	};

	const classes = $derived(
		`inline-flex items-center justify-center gap-2 rounded-control font-medium transition focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg focus-visible:outline-none disabled:opacity-40 ${sizes[size]} ${variants[variant]} ${klass}`
	);
</script>

{#if href}
	<a {href} data-sveltekit-reload={reload ? '' : undefined} class={classes}>
		{@render children()}
	</a>
{:else}
	<button {type} disabled={disabled || loading} {onclick} class={classes}>
		{#if loading}
			<LoaderCircle class="h-4 w-4 animate-spin" />
		{/if}
		{@render children()}
	</button>
{/if}
