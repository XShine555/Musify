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

	const sizes = {
		sm: 'px-4 py-2.5 text-[12.5px]',
		md: 'px-5 py-3 text-[13px]',
		lg: 'px-6 py-3.5 text-sm'
	};

	const subtleButton = 'bg-white/[6%] text-fg-2 hover:bg-white/[9%] hover:text-fg';

	const variants = {
		primary: subtleButton,
		secondary: subtleButton,
		accent: 'bg-accent-btn text-accent-soft hover:bg-accent-btn-hover',
		danger: 'bg-danger text-on-accent hover:brightness-110'
	};

	const classes = $derived(
		`inline-flex items-center justify-center gap-2 rounded-xl font-semibold transition focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-bg focus-visible:outline-none disabled:opacity-40 ${sizes[size]} ${variants[variant]} ${klass}`
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
