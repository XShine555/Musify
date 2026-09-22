<script lang="ts">
	import type { Snippet } from 'svelte';
	import MediaIdentity from './MediaIdentity.svelte';
	import { pressable } from '$lib/actions/pressable';

	interface Props {
		href?: string;
		onclick?: () => void;
		oncontextmenu?: (event: MouseEvent) => void;
		active?: boolean;
		size?: 'xs' | 'sm' | 'lg';
		variant?: 'row' | 'card';
		title: string;
		subtitle?: string | null;
		subtitleHref?: string | number | null;
		explicit?: boolean;
		trackId?: string | number;
		trackIds?: (string | number)[];
		coverSrc?: string | null;
		art?: Snippet;
		overlay?: Snippet;
		leading?: Snippet;
		trailing?: Snippet;
		class?: string;
		style?: string;
	}

	let {
		href,
		onclick,
		oncontextmenu,
		active = false,
		size = 'sm',
		variant = 'row',
		title,
		subtitle,
		subtitleHref,
		explicit = false,
		trackId,
		trackIds,
		coverSrc,
		art,
		overlay,
		leading,
		trailing,
		class: klass = '',
		style
	}: Props = $props();

	const classes = $derived(
		`flex items-center gap-3 text-left transition-colors ${
			variant === 'card' ? 'rounded-art p-2 pr-3.5' : 'rounded-control p-2'
		} ${active ? 'bg-accent-tint' : 'hover:bg-hover'} ${klass}`
	);
</script>

{#snippet content()}
	{@render leading?.()}
	<MediaIdentity
		{title}
		artist={subtitle}
		ownerUserId={subtitleHref}
		{trackId}
		{trackIds}
		{coverSrc}
		{size}
		{explicit}
		{active}
		{art}
		{overlay}
	/>
	{@render trailing?.()}
{/snippet}

{#if href}
	<a {href} {oncontextmenu} class={classes} {style}>
		{@render content()}
	</a>
{:else}
	<div
		role="button"
		tabindex="0"
		use:pressable={onclick ?? (() => {})}
		{oncontextmenu}
		class={classes}
		{style}
	>
		{@render content()}
	</div>
{/if}
