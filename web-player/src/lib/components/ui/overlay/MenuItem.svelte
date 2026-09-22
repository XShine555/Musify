<script lang="ts">
	import type { LucideIcon } from '@lucide/svelte';

	interface Props {
		icon: LucideIcon;
		label: string;
		href?: string;
		target?: string;
		type?: 'button' | 'submit';
		reload?: boolean;
		onclick?: (event: MouseEvent) => void;
		class?: string;
	}

	let {
		icon: Icon,
		label,
		href,
		target,
		type = 'button',
		reload = false,
		onclick,
		class: klass = ''
	}: Props = $props();

	const classes = $derived(
		`flex w-full items-center gap-3 rounded-control px-2 py-2.5 text-left text-sm font-medium text-fg-2 transition hover:bg-hover ${klass}`
	);
</script>

{#if href}
	<a
		{href}
		{target}
		rel={target === '_blank' ? 'noopener noreferrer' : undefined}
		data-sveltekit-reload={reload ? '' : undefined}
		{onclick}
		class={classes}
	>
		<Icon class="size-icon-md shrink-0" />
		{label}
	</a>
{:else}
	<button {type} {onclick} class={classes}>
		<Icon class="size-icon-md shrink-0" />
		{label}
	</button>
{/if}
