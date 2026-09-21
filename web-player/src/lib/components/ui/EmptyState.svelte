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
		<Icon class="size-icon-xl text-fg-2" strokeWidth={1.25} />
	{/if}
	{#if title}
		<h3 class="mt-6 text-display-3 text-fg">
			{title}
		</h3>
	{/if}
	{#if description}
		<p class="mt-2.5 max-w-sm text-sm leading-relaxed text-fg-2">{description}</p>
	{/if}
	{#if actions}
		<div class="mt-6 flex flex-wrap items-center justify-center gap-2.5">{@render actions()}</div>
	{/if}
</div>
