<script lang="ts">
	import type { LucideIcon } from '@lucide/svelte';

	interface Props {
		icon: LucideIcon;
		label: string;
		href?: string;
		type?: 'button' | 'submit';
		reload?: boolean;
		onclick?: (event: MouseEvent) => void;
		class?: string;
	}

	let {
		icon: Icon,
		label,
		href,
		type = 'button',
		reload = false,
		onclick,
		class: klass = ''
	}: Props = $props();

	const classes = $derived(
		`flex w-full items-center gap-3 rounded-control px-2 py-2.5 text-left text-base text-fg-2 transition hover:bg-hover ${klass}`
	);
</script>

{#if href}
	<a {href} data-sveltekit-reload={reload ? '' : undefined} {onclick} class={classes}>
		<Icon class="h-6 w-6 shrink-0" strokeWidth={2} />
		{label}
	</a>
{:else}
	<button {type} {onclick} class={classes}>
		<Icon class="h-6 w-6 shrink-0" strokeWidth={2} />
		{label}
	</button>
{/if}
