<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		selected?: boolean;
		count?: number;
		href?: string;
		onclick?: () => void;
		children: Snippet;
	}

	let { selected = false, count, href, onclick, children }: Props = $props();

	const classes = $derived(
		href
			? 'rounded-full border border-line-strong px-3.5 py-2 text-xs text-fg-2 transition-colors hover:bg-hover hover:text-fg'
			: `flex items-center gap-2 rounded-full px-3.5 py-2 text-xs font-medium transition ${
					selected
						? 'bg-cta-strong text-ink'
						: 'bg-surface-2 text-fg-2 hover:bg-surface-hover hover:text-fg'
				}`
	);
</script>

{#snippet content()}
	{@render children()}
	{#if count !== undefined}
		<span class="tabular-nums {selected ? 'opacity-55' : 'text-muted'}">{count}</span>
	{/if}
{/snippet}

{#if href}
	<a {href} class={classes}>{@render content()}</a>
{:else}
	<button type="button" {onclick} class={classes}>{@render content()}</button>
{/if}
