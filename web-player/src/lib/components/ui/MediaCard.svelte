<script lang="ts" module>
	export const mediaCardArt =
		'aspect-square w-full rounded-art shadow-art transition duration-300 ease-out group-hover:shadow-art-lg';
</script>

<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		href: string;
		title: string;
		subtitle?: string | null;
		index?: number;
		oncontextmenu?: (event: MouseEvent) => void;
		art: Snippet<[string]>;
	}

	let { href, title, subtitle, index = 0, oncontextmenu, art }: Props = $props();
</script>

<a
	{href}
	class="group animate-enter min-w-0 text-left"
	style="animation-delay:{index * 45}ms"
	{oncontextmenu}
>
	{@render art(mediaCardArt)}
	<div class="mt-2.5 truncate text-sm font-medium text-fg">{title}</div>
	{#if subtitle}
		<div class="mt-0.5 truncate text-[12.5px] text-fg-3">{subtitle}</div>
	{/if}
</a>
