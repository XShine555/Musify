<script lang="ts">
	import type { Snippet } from 'svelte';
	import Artwork from './Artwork.svelte';
	import ExplicitBadge from './ExplicitBadge.svelte';
	import ArtistLink from './ArtistLink.svelte';
	import { pressable } from '$lib/actions/pressable';

	interface Props {
		trackId: string | number;
		title: string;
		artist?: string | null;
		ownerUserId?: string | number | null;
		coverSrc?: string | null;
		size?: 'sm' | 'lg';
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
		size = 'sm',
		explicit = false,
		active = false,
		titleClass = '',
		onClick,
		overlay,
		badge
	}: Props = $props();
</script>

<!-- svelte-ignore a11y_click_events_have_key_events -->
<div
	role="button"
	tabindex="0"
	use:pressable={onClick}
	onclick={(event) => event.stopPropagation()}
	class="flex min-w-0 items-center gap-3.5 text-left"
>
	<Artwork trackIds={[trackId]} src={coverSrc} {size} alt={title} class="shrink-0">
		{@render overlay?.()}
	</Artwork>
	<div class="min-w-0">
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
