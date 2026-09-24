<script lang="ts">
	import Plus from '@lucide/svelte/icons/plus';

	interface Props {
		title: string;
		subtitle?: string | null;
		onclick: () => void;
		index?: number;
	}

	let { title, subtitle, onclick, index = 0 }: Props = $props();
</script>

<button
	type="button"
	{onclick}
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
</button>
