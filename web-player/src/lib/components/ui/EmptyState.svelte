<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { LucideIcon } from '@lucide/svelte';

	interface Props {
		icon?: LucideIcon;
		title?: string;
		description?: string;
		class?: string;
		actions?: Snippet;
	}

	let { icon: Icon, title, description, class: klass = '', actions }: Props = $props();
</script>

<div class="flex flex-col items-center rounded-panel px-6 py-16 text-center {klass}">
	{#if Icon}
		<Icon class="h-9 w-9 text-fg-2" strokeWidth={1.3} />
	{/if}
	{#if title}
		<h3 class="mt-6 font-display text-xl font-semibold tracking-[-0.025em] text-fg">
			{title}
		</h3>
	{/if}
	{#if description}
		<p class="mt-2.5 max-w-sm text-[13px] leading-[1.65] text-fg-2">{description}</p>
	{/if}
	{#if actions}
		<div class="mt-6 flex flex-wrap items-center justify-center gap-2.5">{@render actions()}</div>
	{/if}
</div>
