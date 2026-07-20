<script lang="ts">
	import type { Snippet } from 'svelte';
	import Cover from './Cover.svelte';
	import ExplicitBadge from './ExplicitBadge.svelte';

	interface Props {
		trackId: string | number;
		title: string;
		artist?: string;
		coverSrc?: string;
		coverSize?: string;
		hue: number;
		explicit?: boolean;
		titleClass?: string;
		onClick: () => void;
		overlay?: Snippet;
		badge?: Snippet;
	}

	let {
		trackId,
		title,
		artist,
		coverSrc,
		coverSize = 'h-[42px] w-[42px]',
		hue,
		explicit = false,
		titleClass = '',
		onClick,
		overlay,
		badge
	}: Props = $props();
</script>

<button type="button" onclick={onClick} class="flex min-w-0 items-center gap-3.5 text-left">
	<Cover
		{trackId}
		src={coverSrc}
		{hue}
		size="small"
		alt={title}
		class="{coverSize} flex-shrink-0 rounded-control"
	>
		{@render overlay?.()}
	</Cover>
	<div class="min-w-0 flex-1">
		<div class="flex min-w-0 items-center gap-2">
			{#if explicit}
				<ExplicitBadge />
			{/if}
			<span class="min-w-0 truncate text-fg {titleClass}">{title}</span>
			{@render badge?.()}
		</div>
		{#if artist}
			<div class="truncate text-left text-sm text-fg-3">{artist}</div>
		{/if}
	</div>
</button>
