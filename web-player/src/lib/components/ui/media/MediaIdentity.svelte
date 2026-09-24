<script lang="ts">
	import type { Snippet } from 'svelte';
	import Artwork from './Artwork.svelte';
	import ExplicitBadge from './ExplicitBadge.svelte';
	import ArtistLink from './ArtistLink.svelte';

	interface Props {
		trackId?: string;
		trackIds?: string[];
		title: string;
		artist?: string | null;
		ownerUserId?: string | null;
		coverSrc?: string | null;
		size?: 'xs' | 'sm' | 'lg';
		explicit?: boolean;
		active?: boolean;
		titleClass?: string;
		class?: string;
		art?: Snippet;
		overlay?: Snippet;
	}

	let {
		trackId,
		trackIds,
		title,
		artist,
		ownerUserId,
		coverSrc,
		size = 'sm',
		explicit = false,
		active = false,
		titleClass = '',
		class: klass = '',
		art,
		overlay
	}: Props = $props();
</script>

<div class="flex min-w-0 flex-1 items-center gap-3.5 {klass}">
	{#if art}
		{@render art()}
	{:else if trackId !== undefined || coverSrc || trackIds?.length}
		<Artwork
			trackIds={trackIds ?? (trackId !== undefined ? [trackId] : [])}
			src={coverSrc}
			{size}
			alt={title}
			class="shrink-0"
		>
			{@render overlay?.()}
		</Artwork>
	{/if}
	<div class="min-w-0">
		<div class="flex min-w-0 items-center gap-2">
			{#if explicit}
				<ExplicitBadge />
			{/if}
			<span class="min-w-0 truncate text-sm {active ? 'text-accent-soft' : 'text-fg'} {titleClass}"
				>{title}</span
			>
		</div>
		<ArtistLink name={artist} {ownerUserId} class="mt-0.5 text-xs text-fg-2" />
	</div>
</div>
