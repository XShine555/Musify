<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		selected?: boolean;
		disabled?: boolean;
		count?: number;
		href?: string;
		onclick?: () => void;
		children: Snippet;
	}

	let { selected = false, disabled = false, count, href, onclick, children }: Props = $props();

	const classes = $derived(
		href
			? 'rounded-full border border-line-strong px-3.5 py-2 text-xs text-fg-2 transition-colors hover:bg-hover hover:text-fg'
			: `flex items-center gap-2 rounded-full px-3.5 py-2 text-xs font-medium transition ${
					selected ? 'bg-cta-strong text-ink' : 'bg-btn text-fg-2 hover:bg-btn-hover hover:text-fg'
				} ${disabled ? 'cursor-not-allowed opacity-40' : ''}`
	);
</script>

{#snippet content()}
	{@render children()}
	{#if count !== undefined}
		<span class="tabular-nums {selected ? 'opacity-55' : 'text-fg-2'}">{count}</span>
	{/if}
{/snippet}

{#if href}
	<a {href} class={classes}>{@render content()}</a>
{:else}
	<button type="button" {onclick} {disabled} class={classes}>{@render content()}</button>
{/if}
