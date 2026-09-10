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

<div
	class="flex flex-col items-center rounded-panel border border-dashed border-line px-6 py-16 text-center {klass}"
>
	{#if Icon}
		<span
			class="grid h-16 w-16 place-items-center rounded-full text-on-accent"
			style="background:radial-gradient(circle at 30% 30%, color-mix(in oklch, var(--color-accent), white 15%), var(--color-accent))"
		>
			<Icon class="h-7 w-7" />
		</span>
	{/if}
	{#if title}
		<h3 class="mt-6 font-display text-2xl font-medium tracking-tight text-fg">{title}</h3>
	{/if}
	{#if description}
		<p class="mt-2 max-w-sm text-fg-2">{description}</p>
	{/if}
	{#if actions}
		<div class="mt-6 flex items-center gap-3">{@render actions()}</div>
	{/if}
</div>
