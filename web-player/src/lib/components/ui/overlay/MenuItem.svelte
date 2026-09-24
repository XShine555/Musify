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
		`flex w-full items-center gap-3 rounded-control px-2 py-2.5 text-left text-xs font-medium text-fg-2 transition hover:bg-hover ${klass}`
	);
</script>

<!-- svelte-ignore a11y_no_static_element_interactions -->
<svelte:element
	this={href ? 'a' : 'button'}
	{href}
	{target}
	rel={href && target === '_blank' ? 'noopener noreferrer' : undefined}
	type={href ? undefined : type}
	data-sveltekit-reload={href && reload ? '' : undefined}
	{onclick}
	class={classes}
>
	<Icon class="size-icon-md shrink-0" />
	{label}
</svelte:element>
