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

<div class="flex flex-col items-center px-6 py-14 text-center {klass}">
	{#if Icon}
		<span class="grid h-14 w-14 place-items-center rounded-full bg-accent-soft text-on-accent">
			<Icon class="h-6 w-6" />
		</span>
	{/if}
	{#if title}
		<h3 class="mt-5 text-2xl font-bold tracking-tight text-fg">{title}</h3>
	{/if}
	{#if description}
		<p class="mt-2 text-fg-2">{description}</p>
	{/if}
	{#if actions}
		<div class="mt-6 flex items-center gap-3">{@render actions()}</div>
	{/if}
</div>
