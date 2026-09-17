<script lang="ts">
	import type { Snippet } from 'svelte';
	import Cover from './Cover.svelte';
	import ExplicitBadge from './ExplicitBadge.svelte';
	import ArtistLink from './ArtistLink.svelte';

	interface Props {
		trackId: string | number;
		title: string;
		artist?: string | null;
		ownerUserId?: string | number | null;
		coverSrc?: string | null;
		coverSize?: string;
		explicit?: boolean;
		active?: boolean;
		titleClass?: string;
		onClick: () => void;
		overlay?: Snippet;
		badge?: Snippet;
	}

	let {
		trackId,
		title,
		artist,
		ownerUserId,
		coverSrc,
		coverSize = 'h-10.5 w-10.5',
		explicit = false,
		active = false,
		titleClass = '',
		onClick,
		overlay,
		badge
	}: Props = $props();

	function onKeydown(event: KeyboardEvent) {
		if (event.key !== 'Enter' && event.key !== ' ') return;
		event.preventDefault();
		onClick();
	}

	function onclick(event: MouseEvent) {
		event.stopPropagation();
		onClick();
	}
</script>

<div
	role="button"
	tabindex="0"
	{onclick}
	onkeydown={onKeydown}
	class="flex min-w-0 items-center gap-3.5 text-left"
>
	<Cover
		{trackId}
		src={coverSrc}
		size="small"
		alt={title}
		class="{coverSize} shrink-0 rounded-control"
	>
		{@render overlay?.()}
	</Cover>
	<div class="min-w-0 flex-1">
		<div class="flex min-w-0 items-center gap-2">
			{#if explicit}
				<ExplicitBadge />
			{/if}
			<span class="min-w-0 truncate text-sm {active ? 'text-accent-soft' : 'text-fg'} {titleClass}"
				>{title}</span
			>
			{@render badge?.()}
		</div>
		<ArtistLink name={artist} {ownerUserId} class="mt-0.5 text-xs text-fg-3" />
	</div>
</div>
