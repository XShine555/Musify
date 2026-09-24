<script lang="ts">
	import Plus from '@lucide/svelte/icons/plus';

	interface Props {
		title: string;
		subtitle?: string | null;
		href: string;
		index?: number;
	}

	let { title, subtitle, href, index = 0 }: Props = $props();
</script>

<a
	{href}
	class="group animate-enter min-w-0 rounded-panel p-2.5 text-left focus-ring transition-colors hover:bg-hover"
	style="--i:{index}"
>
	<div
		class="grid aspect-square w-full place-items-center rounded-art bg-[image:var(--mf-liked-grad)]"
	>
		<Plus class="size-icon-xl text-on-art" />
	</div>
	<div class="mt-2.5 truncate text-sm text-fg">{title}</div>
	{#if subtitle}
		<div class="mt-0.5 truncate text-xs text-fg-2">{subtitle}</div>
	{/if}
</a>
